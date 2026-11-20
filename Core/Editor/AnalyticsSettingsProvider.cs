using System.Collections.Generic;
using System.Linq;
using RiseOn.Utils.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RiseOn.Analytics.Editor {
    /// <summary>
    /// Project Settings > RiseOn > Analytics: what blocks the build, what the studio's other packs report by themselves, the services, each with its package, its own<br/>
    /// fields and the events it takes, the Editor's Console first, then the game's events.<br/>
    /// Edits go straight to the asset, with undo; the events file waits for Apply or for the page to close, since writing it recompiles.<br/>
    /// The settings asset's Inspector only opens this page.
    /// </summary>
    internal static class AnalyticsSettingsProvider {
        internal const string PagePath = "Project/RiseOn/Analytics";

        private static readonly (string Name, bool Value)[] EventModes = { ("All", false), ("Selected", true) };
        private const string EventsTooltip = "The events the service takes; ad revenue, purchases and user data go to every service whatever this says.";

        // Each service's list of events, by its id, as a list keeps its own drag.
        private static readonly Dictionary<string, SectionRows> sentRows = new();

        private static readonly SettingsTree tree = new();
        private static readonly SectionRows  rows = new();

        // Whether the Events section took its kept fold since the page opened.
        private static bool restored;

        // A recompile closes the page as the domain unloads: a write then would land after the compile and wait,
        // unseen, for the next one. The page comes back after the reload and still says the file waits.
        private static bool reloading;

        [InitializeOnLoadMethod]
        private static void WatchReload() => AssemblyReloadEvents.beforeAssemblyReload += () => reloading = true;

        // The pack's services, each listed whether its package is in the project or not, with its folder in the Analytics repo, beside Core.
        private static readonly PackAdapters services = new("RiseOn.Analytics.Services", typeof(AnalyticsSettings)
                                                          , new PackAdapter("AppsFlyer", "com.riseon.analytics.appsflyer", "AppsFlyer")
                                                          , new PackAdapter("Google Analytics", "com.riseon.analytics.firebase", "Firebase"));

        [MenuItem("RiseOn/Analytics")]
        internal static void Open() {
            SettingsService.OpenProjectSettings(PagePath);
        }

        [SettingsProvider]
        private static SettingsProvider Create() {
            // Unity deactivates the page it rebuilds after a reload before it draws; only a drawn page has edits to write.
            var drawn = false;

            return new SettingsProvider(PagePath, SettingsScope.Project) {
                guiHandler = _ => {
                    using var page = SettingsPage.Begin();
                    AnalyticsEventsFile.Held = true;
                    drawn = true;
                    Draw(LoadOrCreate());
                }
              , deactivateHandler = () => {
                    // Leaving or closing the window asks once, for every edit made here: one write and one recompile, or none.
                    AnalyticsEventsFile.Held = false;
                    var settings = Find();
                    if (settings && drawn && !reloading && AnalyticsEventsFile.IsPending(settings)) AskOnLeave(settings);
                    drawn    = false;
                    restored = false;
                    tree.Dispose();
                }
              , keywords = new[] { "analytics", "event", "firebase", "appsflyer", "revenue" }
            };
        }

        /// <summary>The whole page for one settings asset.</summary>
        internal static void Draw(AnalyticsSettings settings) {
            SettingsIssue.Draw(Issues());
            DrawApply(settings);

            var root = tree.Begin(settings);
            using (new Section("Integrations", "What the studio's other packs report by themselves, with no call from the game.")) {
                DrawFromPack(root.Children["adRevenueFromAds"], "com.riseon.ads", AdsInstalled);
                DrawFromPack(root.Children["purchasesFromIap"], "com.riseon.iap", IapInstalled);
            }

            DrawServices(settings, root);
            DrawEventList(settings, root.Children["events"]);
            tree.End();
        }

        private const string ServicesKey = "RiseOn.Analytics.Services";

        private static readonly SectionRows serviceRows = new();

        // A row of the services: the Editor, a service the project has, or one of the pack's it lacks, which has no id.
        private readonly struct ServiceRow {
            public readonly string                  Title;
            public readonly string                  Id;
            public readonly AnalyticsEditor.Service Service;
            public readonly PackAdapter?            Adapter;
            public readonly PackageRow              Package;

            public ServiceRow(string title, string id, AnalyticsEditor.Service service, PackAdapter? adapter, PackageRow package) {
                Title   = title;
                Id      = id;
                Service = service;
                Adapter = adapter;
                Package = package;
            }
        }

        // As the Ads page's mediations: one section, a row each, the version and its buttons in the value column, Update All on the bar.
        private static void DrawServices(AnalyticsSettings settings, InspectorProperty root) {
            services.Load();
            using var section = Section.Foldout(ServicesKey, "Services", "Where the events go: the Editor's Console, then each analytics service, with its package.", buttons: 1, padded: false, barValue: true);
            using (new EditorGUI.DisabledScope(services.IsLoading)) {
                if (section.Button(0, SdfIconType.ArrowClockwise, "Read the services' versions again")) services.Reload();
            }

            var shown = ServiceRows();
            services.DrawUpdateAll(section.BarValue, shown.Where(row => row.Adapter != null && row.Id != null).Select(row => (row.Adapter.Value, row.Package)));
            if (!section.Open) return;

            PackageJobs.DrawRunning(services.Owner);
            serviceRows.Draw(shown.Count, index => DrawService(settings, root, shown[index]), "None");
        }

        // The Editor first, as every project has it; then every service the project has and every one of the pack's it lacks, by name.
        private static List<ServiceRow> ServiceRows() {
            var shown = new List<ServiceRow> { new("Editor", AnalyticsSdk.EditorSinkId, null, null, null) };
            var held = AnalyticsEditor.Services.Select(service => {
                var adapter = services.All.Where(entry => entry.Package == service.Package).Select(entry => (PackAdapter?)entry).FirstOrDefault();
                return new ServiceRow(service.Title, service.Id, service, adapter, adapter is { } own ? services.Row(own) : null);
            });
            var missing = services.All.Where(adapter => AnalyticsEditor.Services.All(service => service.Package != adapter.Package)).Select(adapter => new ServiceRow(adapter.Name, null, null, adapter, services.Missing(adapter)));
            shown.AddRange(held.Concat(missing).OrderBy(row => row.Title, System.StringComparer.Ordinal));
            return shown;
        }

        private static readonly GUIContent EditorTitle = new("Editor", "The Editor's Console, which prints what the game logs; the Editor sends nothing to the services, as their SDKs are native.");

        // At the value column's end, as a package's Uninstall: the Editor's Console on or off. Off, its row stays folded, as nothing prints.
        private static bool DrawEnable(Rect value, InspectorProperty toggle) {
            var on     = toggle.ValueEntry.WeakSmartValue is true;
            var why    = toggle.GetAttribute<TooltipAttribute>()?.tooltip;
            var label  = new GUIContent(on ? "Disable" : "Enable", on ? $"Stop the Console printing. {why}" : $"Print to the Console. {why}");
            var width  = PackageRow.ButtonWidth(label);
            if (!GUI.Button(new Rect(value.xMax - width, value.y, width, value.height), label, EditorStyles.miniButton)) return on;

            toggle.ValueEntry.WeakSmartValue = !on;
            return !on;
        }

        // The name in the label column, folding the service's events and fields under it; one with a package has its version and buttons in
        // the value column. One of the pack's not in the project is its name alone.
        private static void DrawService(AnalyticsSettings settings, InspectorProperty root, ServiceRow row) {
            var line  = EditorGUILayout.GetControlRect();
            var value = SectionRows.MarkFirstRow(line, out var prefix);
            if (row.Package != null) PackageRow.Draw(value, row.Package);
            if (row.Id == null) {
                GUI.Label(prefix, new GUIContent(row.Title, row.Package?.Tooltip));
                return;
            }

            // An issue's Edit opens the row that holds its field, as a section opens for one.
            var key  = $"{ServicesKey}.{row.Id}";
            var open = SessionState.GetBool(key, false) || SettingsFocus.Asset is { } target && row.Service?.Issues?.Invoke().Any(issue => issue.FieldAsset == target) == true;
            if (row.Service == null) {
                var was = root.Children["logInEditor"].ValueEntry.WeakSmartValue is true;
                var on  = DrawEnable(value, root.Children["logInEditor"]);
                // Turning it on opens the row, as a toggled section opens.
                if (on && !was) open = true;
                using (new EditorGUI.DisabledScope(!on)) open = EditorGUI.Foldout(prefix, on && open, EditorTitle, true) && on;
            } else {
                open = EditorGUI.Foldout(row.Package == null ? line : prefix, open, new GUIContent(row.Title, row.Service.Tooltip), true);
            }

            SessionState.SetBool(key, open);

            // Under the name, as Unity indents what a foldout opens, with the fade the page's sections use.
            if (Section.BeginFade(key, open)) {
                var width = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = width - Section.Indent;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(Section.Indent);
                EditorGUILayout.BeginVertical();
                // The service's own fields first, the events it takes, a list that grows, last.
                row.Service?.Draw?.Invoke();
                DrawEvents(settings, root, row.Id);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = width;
            }

            Section.EndFade();
        }

        // A package change reloads the domain, which asks again.
        private static readonly bool AdsInstalled = UnityEditor.PackageManager.PackageInfo.FindForPackageName("com.riseon.ads") != null;
        private static readonly bool IapInstalled = UnityEditor.PackageManager.PackageInfo.FindForPackageName("com.riseon.iap") != null;

        // Without the package nothing reports to send, so the toggle shows off and cannot change.
        private static void DrawFromPack(InspectorProperty property, string package, bool installed) {
            if (installed) {
                property.Draw();
                return;
            }

            using (new EditorGUI.DisabledScope(true)) {
                EditorGUILayout.Toggle(new GUIContent(property.GetAttribute<Sirenix.OdinInspector.LabelTextAttribute>()?.Text ?? property.NiceName, $"Needs {package}, which this project does not have."), false);
            }
        }

        // The page's edits until they are applied or reverted; otherwise Unity stopping before the write, or a file edited by hand.
        private static void DrawApply(AnalyticsSettings settings) {
            if (AnalyticsEventsFile.IsCurrent(settings)) {
                AnalyticsEventsFile.Remember(settings);
                return;
            }

            if (!AnalyticsEventsFile.IsPending(settings)) return;

            var renames = AnalyticsEventsFile.Renames(settings);
            var renamed = renames.Count == 0 ? "" : $" Apply also renames {string.Join(", ", renames.Select(rename => $"AnalyticsEvent.{rename.From} to {rename.To}"))} in the project's scripts.";
            var revert  = AnalyticsEventsFile.RevertProblem();
            var picked  = HelpBoxUtils.Draw($"{AnalyticsEventsFile.FilePath} does not list the events yet, so AnalyticsEvent is out of date and builds stop. Apply writes it; Revert takes the events back to the last time they matched it.{renamed}", MessageType.Warning,
                // Revert then Apply, as Unity's import settings order them.
                (new GUIContent("Revert", revert ?? "Takes the events back to the last time they matched the events file"), revert == null), (new GUIContent("Apply", "Writes the events file; Unity recompiles AnalyticsEvent"), true));
            if (picked == 1) AnalyticsEventsFile.Sync(settings);
            if (picked != 0) return;

            AnalyticsEventsFile.Revert(settings);
            GUIUtility.ExitGUI();
        }

        // The events the game logs, one row each, added with the bar's +.
        private static void DrawEventList(AnalyticsSettings settings, InspectorProperty events) {
            if (!restored) {
                Section.Restore(events);
                restored = true;
            }

            var resolver = (IOrderedCollectionResolver) events.ChildResolver;
            using var section = Section.Foldout(events, "Events", "What the game logs with AnalyticsSdk.Log, as AnalyticsEvent members once applied.", buttons: 1, padded: false);
            if (section.Button(0, SdfIconType.Plus, "Add an event")) {
                resolver.QueueAdd(new object[] { new AnalyticsEventEntry { Id = AnalyticsEventsFile.NewId(settings), Name = UnusedName(settings) } });
                // The new row shows, on a folded section too.
                Section.SetExpandedRecursively(events, true);
            }

            if (!section.Open) return;

            rows.Draw(events.Children.Count, i => DrawEvent(events.Children[i]), "None", (from, to) => {
                // Odin's resolver has no move, so the event leaves its place and comes back at the other.
                var moved = events.Children[from].ValueEntry.WeakSmartValue;
                resolver.QueueRemoveAt(from);
                resolver.QueueInsertAt(to, new[] { moved });
            }, resolver.QueueRemoveAt, "Remove the event");
        }

        // Puts @ before a C# keyword, as the pack's source generator does.
        private static readonly Microsoft.CSharp.CSharpCodeProvider CSharp = new();

        private const float NameLabel        = 45;
        private const float DescriptionLabel = 75;

        // An event on one line, its name then its description, written through the value entries, with undo; its id is for enum fields only.
        private static void DrawEvent(InspectorProperty entry) {
            var name        = entry.Children["name"];
            var description = entry.Children["description"];

            var line            = EditorGUILayout.GetControlRect();
            var nameRect        = new Rect(line.x, line.y, line.width * 0.4f - 4, line.height);
            var nameField       = Rect.MinMaxRect(nameRect.x + NameLabel, line.y, nameRect.xMax, line.yMax);
            var descriptionRect = Rect.MinMaxRect(nameRect.xMax + 4, line.y, line.xMax, line.yMax);
            var descriptionText = Rect.MinMaxRect(descriptionRect.x + DescriptionLabel, line.y, line.xMax, line.yMax);

            var text = name.ValueEntry.WeakSmartValue as string ?? "";
            EditorGUI.LabelField(new Rect(nameRect.x, line.y, NameLabel, line.height), new GUIContent("Name", $"What the services get; AnalyticsEvent.{CSharp.CreateEscapedIdentifier(text)} in code"));
            EditorGUI.BeginChangeCheck();
            var newName = EditorGUI.DelayedTextField(nameField, text);
            if (EditorGUI.EndChangeCheck()) name.ValueEntry.WeakSmartValue = newName.Trim();
            SettingsFocus.Drawn((UnityEngine.Object)entry.Tree.WeakTargets[0], name.UnityPropertyPath, nameField);

            EditorGUI.LabelField(new Rect(descriptionRect.x, line.y, DescriptionLabel, line.height), new GUIContent("Description", "Shown on the enum member in code"));
            EditorGUI.BeginChangeCheck();
            var newDescription = EditorGUI.DelayedTextField(descriptionText, description.ValueEntry.WeakSmartValue as string ?? "");
            if (EditorGUI.EndChangeCheck()) description.ValueEntry.WeakSmartValue = newDescription;
        }

        private static string UnusedName(AnalyticsSettings settings) {
            for (var i = 1;; i++) {
                var name = $"new_event_{i}";
                if (settings.Events.All(entry => entry.Name != name)) return name;
            }
        }

        // Discard needs what the file last matched; without it the choice is to apply now or leave the file waiting, with builds stopped.
        private static void AskOnLeave(AnalyticsSettings settings) {
            const string message = "The events changed since AnalyticsEvent was last written.";
            var revert = AnalyticsEventsFile.RevertProblem();
            if (revert == null) {
                if (EditorUtility.DisplayDialog("Analytics", message, "Apply", "Discard")) AnalyticsEventsFile.Sync(settings);
                else AnalyticsEventsFile.Revert(settings);
            } else if (EditorUtility.DisplayDialog("Analytics", $"{message} They cannot be discarded: {revert.Substring(revert.IndexOf('\n') + 1)}", "Apply", "Later")) {
                AnalyticsEventsFile.Sync(settings);
            }
        }

        // The service's entry, made the first time the page changes it; without one it takes every event.
        private static void DrawEvents(AnalyticsSettings settings, InspectorProperty root, string id) {
            var index = settings.Sinks.ToList().FindIndex(entry => entry.Sink == id);
            var only  = index >= 0 && settings.Sinks[index].OnlyListed;

            // The tree takes a new entry at its next update.
            var entries = root.Children["sinkEvents"].Children;
            var list    = only && index < entries.Count ? entries[index].Children["events"] : null;

            // The list's own bar picks All or Selected, so All is the bar alone.
            using var section = new Section("Events", EventsTooltip, buttons: list == null ? 0 : 1, padded: false, barValue: true, barOnly: list == null);
            ChoiceFieldUtils.Value(section.BarValue, EventModes, only, picked => {
                Undo.RecordObject(settings, "Change Analytics Events");
                settings.SetOnlyListed(id, picked);
                EditorUtility.SetDirty(settings);
            });
            if (list == null) return;

            var resolver = (IOrderedCollectionResolver) list.ChildResolver;
            var listed   = settings.Sinks[index].Events;
            var unused   = System.Enum.GetValues(typeof(AnalyticsEvent)).Cast<AnalyticsEvent>().Where(value => !listed.Contains(value)).ToList();
            using (new EditorGUI.DisabledScope(unused.Count == 0)) {
                var add = settings.Events.Count == 0 ? "No event yet: add one under Events, then Apply" : unused.Count == 0 ? "Every event is listed" : "Add an event";
                if (section.Button(0, SdfIconType.Plus, add)) resolver.QueueAdd(new object[] { unused[0] });
            }

            if (!sentRows.TryGetValue(id, out var rows)) sentRows[id] = rows = new SectionRows();
            rows.Draw(list.Children.Count, i => ChoiceFieldUtils.Enum(EditorGUILayout.GetControlRect(), list.Children[i]), "None", remove: resolver.QueueRemoveAt, removeTooltip: "Take the event out");
        }

        /// <summary>Everything the installed services lack, for the page and the build.</summary>
        internal static IEnumerable<SettingsIssue> Issues() {
            if (SettingsAssetUtils.OutsideResources<AnalyticsSettings>(AnalyticsSettings.ResourceName) is { } path) {
                yield return new SettingsIssue(BuildTarget.NoTarget, true, $"{path} is outside a Resources folder, so the game cannot load it and sends every event everywhere.", "Move it", () => SettingsAssetUtils.MoveToResources<AnalyticsSettings>(AnalyticsSettings.ResourceName));
            }

            if (Find() is { } settings) {
                foreach (var issue in AnalyticsEventRules.Issues(settings)) yield return issue;
            }

            foreach (var service in AnalyticsEditor.Services) {
                if (service.Issues == null) continue;
                foreach (var issue in service.Issues()) yield return issue;
            }
        }

        internal static AnalyticsSettings LoadOrCreate() => SettingsAssetUtils.LoadOrCreate<AnalyticsSettings>(AnalyticsSettings.ResourceName);

        /// <summary>The project's settings asset wherever the project keeps it; null when it has none.</summary>
        internal static AnalyticsSettings Find() => SettingsAssetUtils.Find<AnalyticsSettings>(AnalyticsSettings.ResourceName);
    }

    [CustomEditor(typeof(AnalyticsSettings))]
    internal sealed class AnalyticsSettingsInspector : SettingsAssetInspector {
        protected override string PagePath => AnalyticsSettingsProvider.PagePath;
    }

    /// <summary>Stops a build that a service's setup would leave without data.</summary>
    internal sealed class AnalyticsBuildCheck : IPreprocessBuildWithReport {
        public int callbackOrder => int.MinValue;

        public void OnPreprocessBuild(BuildReport report) {
            var blocking = AnalyticsSettingsProvider.Issues().Where(issue => issue.Blocking && issue.Concerns(report.summary.platform)).Select(issue => issue.Message).ToList();
            var settings = AnalyticsSettingsProvider.Find();
            if (settings && !AnalyticsEventsFile.IsCurrent(settings)) blocking.Add($"{AnalyticsEventsFile.FilePath} does not list the events, so AnalyticsEvent is out of date: press Apply on the page.");
            if (blocking.Count > 0) throw new BuildFailedException("Analytics: fix these before building, Project Settings > RiseOn > Analytics has a button for some:\n- " + string.Join("\n- ", blocking));
        }
    }
}