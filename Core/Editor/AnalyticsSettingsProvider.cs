using System.Collections.Generic;
using System.Linq;
using RiseOn.Utils.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RiseOn.Analytics.Editor {
    /// <summary>
    /// Project Settings > RiseOn > Analytics: what blocks the build, the services, each with its package and its own fields, the Editor's Console first, then the two tables of events, a column for each service: the revenue each service has a standard call for, with whether the studio's packs send it themselves, and the game's own events.<br/>
    /// Edits go straight to the asset, with undo; the events file waits for Apply or for the page to close, since writing it recompiles.<br/>
    /// The settings asset's Inspector only opens this page.
    /// </summary>
    internal static class AnalyticsSettingsProvider {
        internal const string PagePath = "Project/RiseOn/Analytics";

        private static readonly SettingsTree tree = new();
        private static readonly SectionRows  rows = new();

        // Whether the Events section took its kept fold since the page opened.
        private static bool restored;

        // A recompile closes the page as the domain unloads: a write then would land after the compile and wait,
        // unseen, for the next one. The page comes back after the reload and still says the file waits.
        private static bool reloading;

        [InitializeOnLoadMethod]
        private static void WatchReload() {
            AssemblyReloadEvents.beforeAssemblyReload += () => reloading = true;
        }

        // The pack's services, each listed whether its package is in the project or not, with its folder in the Analytics repo, beside Core.
        private static readonly PackAdapters services = new(typeof(AnalyticsSettings)
                                                          , new PackAdapter("AppsFlyer", "com.riseon.analytics.appsflyer", "AppsFlyer")
                                                          , new PackAdapter("Firebase Analytics", "com.riseon.analytics.firebase", "Firebase"));

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
                    AnalyticsEventsFile.held = true;
                    drawn = true;
                    Draw(LoadOrCreate());
                }
              , deactivateHandler = () => {
                    // Leaving or closing the window asks once, for every edit made here: one write and one recompile, or none.
                    AnalyticsEventsFile.held = false;
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
            DrawServices();
            DrawStandardEvents(settings, root);
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
        private static void DrawServices() {
            services.Load();
            using var section = Section.Foldout(ServicesKey, "Services", "Where the events go: each analytics service, with its package; the Editor's Console needs none.", buttons: 1, padded: false, barValue: true);
            using (new EditorGUI.DisabledScope(services.IsLoading)) {
                if (section.Button(0, SdfIconType.ArrowClockwise, "Read the services' versions again")) services.Reload();
            }

            var shown = ServiceRows();
            services.DrawUpdateAll(section.BarValue, shown.Where(row => row.Adapter != null && row.Id != null).Select(row => (row.Adapter.Value, row.Package)));
            if (!section.Open) return;

            serviceRows.Draw(shown.Count, index => DrawService(shown[index]), "None");
        }

        // Every service the project has and every one of the pack's it lacks, by name.
        private static List<ServiceRow> ServiceRows() {
            var shown = new List<ServiceRow>();
            var held  = AnalyticsEditor.services.Select(service => {
                var adapter = services.All.Where(entry => entry.Package == service.Package).Select(entry => (PackAdapter?)entry).FirstOrDefault();
                return new ServiceRow(service.Title, service.Id, service, adapter, adapter is { } own ? services.Row(own) : null);
            });
            var missing = services.All.Where(adapter => AnalyticsEditor.services.All(service => service.Package != adapter.Package)).Select(adapter => new ServiceRow(adapter.Name, null, null, adapter, services.Missing(adapter)));
            shown.AddRange(held.Concat(missing).OrderBy(row => row.Title, System.StringComparer.Ordinal));
            return shown;
        }

        // The name in the label column; one with a package has its version and buttons in the value column. A service with fields of its
        // own folds them under its name; one without, and one of the pack's not in the project, are the name alone.
        private static void DrawService(ServiceRow row) {
            var line  = EditorGUILayout.GetControlRect();
            var value = SectionRows.MarkFirstRow(line, out var prefix);
            if (row.Package != null) PackageRow.Draw(value, row.Package);

            // Stopping an inset short of the version, so a long name cut at the column does not run into it.
            var title = new GUIContent(row.Title, row.Service?.Tooltip ?? row.Package?.Tooltip);
            var name  = row.Package == null ? line : Rect.MinMaxRect(prefix.x, prefix.y, Mathf.Max(prefix.x, value.x - Section.Inset), prefix.yMax);
            if (row.Service?.Draw == null) {
                GUI.Label(name, title, Section.CutLabel);
                return;
            }

            // An issue's Edit opens the row that holds its field, as a section opens for one.
            var key  = $"{ServicesKey}.{row.Id}";
            var open = SessionState.GetBool(key, false) || SettingsFocus.Asset is { } target && row.Service.Issues?.Invoke().Any(issue => issue.FieldAsset == target) == true;
            // The arrow alone, folding on a click anywhere in the rect; the name as a label, which Unity cuts at its rect and a foldout's text it does not.
            open = EditorGUI.Foldout(name, open, GUIContent.none, true);
            GUI.Label(Rect.MinMaxRect(Mathf.Min(name.xMax, name.x + EditorStyles.foldout.padding.left), name.y, name.xMax, name.yMax), title, Section.CutLabel);
            SessionState.SetBool(key, open);

            // Under the name, as Unity indents what a foldout opens, with the fade the page's sections use.
            if (Section.BeginFade(key, open)) {
                var width = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = width - Section.Indent;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(Section.Indent);
                EditorGUILayout.BeginVertical();
                row.Service.Draw();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = width;
            }

            Section.EndFade();
        }

        // A package change reloads the domain, which asks again.
        private static readonly bool adsInstalled = UnityEditor.PackageManager.PackageInfo.FindForPackageName("com.riseon.ads") != null;
        private static readonly bool iapInstalled = UnityEditor.PackageManager.PackageInfo.FindForPackageName("com.riseon.iap") != null;


        // After the row's ticks, whether the studio's pack that reports the standard event sends it, its tick after its label, each row's
        // as wide as its own label; where the ticks leave less room, the label is cut short with an ellipsis, as Unity's labels are. Without
        // the package nothing reports it, so it shows off and cannot change; a standard event no pack reports has none.
        private static void DrawFromPack(Rect rect, AnalyticsStandardEvent standard, InspectorProperty root) {
            var (field, package, installed) = standard switch {
                AnalyticsStandardEvent.AdRevenue => ("adRevenueFromAds", "com.riseon.ads", adsInstalled)
              , AnalyticsStandardEvent.Purchase  => ("purchasesFromIap", "com.riseon.iap", iapInstalled)
              , _                                => (null, null, false)
            };
            if (field == null) return;

            var property = root.Children[field];
            var label    = new GUIContent(property.GetAttribute<LabelTextAttribute>().Text, installed ? property.GetAttribute<TooltipAttribute>()?.tooltip : $"Needs {package}, which this project does not have.");
            // Measured unclipped, as a style that cuts with an ellipsis measures only its padding.
            var width    = Mathf.Max(0, Mathf.Min(EditorStyles.label.CalcSize(label).x, rect.width - Section.Inset - TickWidth));
            var text     = new Rect(rect.x, rect.y, width, rect.height);
            var box      = new Rect(text.xMax + (width > 0 ? Section.Inset : 0), rect.y, TickWidth, rect.height);
            var shown    = width > 0;
            using (new EditorGUI.DisabledScope(!installed)) {
                EditorGUI.BeginChangeCheck();
                var on = EditorGUI.Toggle(box, installed && property.ValueEntry.WeakSmartValue is true);
                if (EditorGUI.EndChangeCheck()) property.ValueEntry.WeakSmartValue = on;
                if (shown) GUI.Label(text, label.text, Section.CutLabel);
            }

            GUI.Label(Rect.MinMaxRect(shown ? text.x : box.x, rect.y, box.xMax, rect.yMax), new GUIContent("", label.tooltip));
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

        private const string StandardEventsKey = "RiseOn.Analytics.StandardEvents";

        // A row for each standard event; one without an entry here is named from its member.
        private static readonly Dictionary<AnalyticsStandardEvent, GUIContent> standardEventLabels = new() {
            { AnalyticsStandardEvent.AdRevenue, new GUIContent("Ad Revenue", "Each ad's revenue, from com.riseon.ads while its Link RiseOn Ads is on, or from the game's AnalyticsSdk.LogAdRevenue.") },
            { AnalyticsStandardEvent.Purchase, new GUIContent("Purchase", "Each store purchase, once confirmed, from com.riseon.iap while its Link RiseOn Iap is on, or from the game's AnalyticsSdk.LogPurchase.") }
        };

        // Between two columns, and beside a header turned on its side.
        private const float ColumnGap = 6;

        // The box of a toggle, which Unity draws at its rect's left.
        private const float TickWidth = 15;

        // The value column's width and the window's at the tables' last repaint: a frame lays the headers out by that column moved by as much
        // as the window's width has since, as a window dragged narrower changes it on every frame, so every event of a frame lays out alike;
        // until a repaint the headers lie flat. One for both tables, whose value columns are alike.
        private static (float Column, float View)? lastTable;

        // A column of the tables: the Editor's Console first, then each service the project has, headed by its icon, or by its title for one without.
        private readonly struct Column {
            public readonly string     Id;
            public readonly GUIContent Title;
            public readonly Texture    Icon;

            public Column(string id, GUIContent title, Texture icon) {
                Id    = id;
                Title = title;
                Icon  = icon;
            }

            public bool IsEditor => Id == AnalyticsSdk.EditorSinkId;
        }

        private static List<Column> Columns() {
            // The Console's icon for a logged message, as each line Debug.Log prints has.
            var columns = new List<Column> { new(AnalyticsSdk.EditorSinkId, new GUIContent("Editor", "The Editor's Console, which prints what the game logs while it plays in the Editor; the Editor sends nothing to the services, as their SDKs are native."), EditorGUIUtility.IconContent("console.infoicon").image) };
            columns.AddRange(AnalyticsEditor.services.Select(service => new Column(service.Id, new GUIContent(service.Title, service.Title), service.Icon?.Invoke())));
            return columns;
        }

        // Where a table's ticks go: each column's left edge, the columns' width, and where the space after the last begins.
        private readonly struct Table {
            public readonly List<float> Lefts;
            public readonly float       Cell;

            public Table(List<float> lefts, float cell) {
                Lefts = lefts;
                Cell  = cell;
            }

            public float End => Lefts[Lefts.Count - 1] + Cell + ColumnGap;

            // As wide as the box Unity draws at a toggle's left, so the box is centered under its header.
            public Rect Box(int column, Rect row) {
                return new Rect(Lefts[column] + (Cell - TickWidth) / 2, row.y, TickWidth, row.height);
            }
        }

        // The header row: the columns from the value column's start, every one as wide as the widest header, each centered in its own, an
        // icon a line square, its title over it on hover. Titles of columns without an icon lie flat while side by side they fit; else, as in
        // Unity's layer collision matrix, each stands on its side, as wide as a line, the row as tall as the longest. A title for the label
        // column, past a list's drag handle, and one for what follows the columns, up to a list's x, sit at the row's foot, cut short with an
        // ellipsis where they have less room.
        private static Table DrawHeader(List<Column> columns, bool list, GUIContent label = null, GUIContent after = null) {
            var line   = EditorGUIUtility.singleLineHeight;
            var widths = columns.Select(column => column.Icon ? line : EditorStyles.label.CalcSize(column.Title).x).ToList();
            var widest = widths.Max();
            var view   = EditorGUIUtility.currentViewWidth;
            var need   = widest * columns.Count + ColumnGap * (columns.Count - 1);
            var flat   = need <= (lastTable is { } last ? last.Column + view - last.View : float.MaxValue);
            var cell   = flat ? widest : line + ColumnGap;

            var header = SirenixEditorGUI.PrefixRect(EditorGUILayout.GetControlRect(false, flat ? line : widest + 4), out var prefix);
            // A guess that laid the headers the wrong way lays the window out again.
            if (Event.current.type is EventType.Repaint) {
                lastTable = (header.width, view);
                if (need <= header.width != flat) HandleUtility.Repaint();
            }

            // A list's handle and x stand where a padded row's insets are, and a square wider.
            var square = list ? Section.ButtonWidth + 1 - Section.Inset : 0;
            var lefts  = new List<float>();
            var x      = header.x;
            for (var i = 0; i < columns.Count; i++) {
                lefts.Add(x);
                if (columns[i].Icon) {
                    var icon = new Rect(x + (cell - line) / 2, header.yMax - line, line, line);
                    GUI.DrawTexture(PixelUtils.Snap(icon), columns[i].Icon, ScaleMode.ScaleToFit);
                    GUI.Label(icon, new GUIContent("", columns[i].Title.tooltip));
                } else if (flat) {
                    GUI.Label(new Rect(x + (cell - widths[i]) / 2, header.yMax - line, widths[i], line), columns[i].Title);
                } else {
                    // Turned a quarter left about its bottom left corner, the text reads upward from the row's top.
                    var matrix = GUI.matrix;
                    var foot   = new Vector2(x, header.yMax);
                    GUIUtility.RotateAroundPivot(-90, foot);
                    GUI.Label(new Rect(foot.x, foot.y, widths[i] + 2, line), columns[i].Title);
                    GUI.matrix = matrix;
                }

                x += cell + ColumnGap;
            }

            if (label != null) GUI.Label(Rect.MinMaxRect(Mathf.Min(prefix.x + square, prefix.xMax), header.yMax - line, prefix.xMax, header.yMax), label, Section.CutLabel);
            if (after != null && header.xMax - square > x) GUI.Label(Rect.MinMaxRect(x, header.yMax - line, header.xMax - square, header.yMax), after, Section.CutLabel);
            return new Table(lefts, cell);
        }

        // A tick of a table, its tooltip over it. Returns whether it was clicked.
        private static bool Tick(AnalyticsSettings settings, Rect box, bool on, string tip, out bool next) {
            EditorGUI.BeginChangeCheck();
            next = EditorGUI.Toggle(box, on);
            GUI.Label(box, new GUIContent("", tip));
            if (!EditorGUI.EndChangeCheck()) return false;

            Undo.RecordObject(settings, "Change Analytics Events");
            return true;
        }

        // A row for each standard event: the services, a tick sending the row to the service through its standard call, then whether the
        // studio's pack that reports it sends it.
        private static void DrawStandardEvents(AnalyticsSettings settings, InspectorProperty root) {
            using var section = Section.Foldout(StandardEventsKey, "Standard Events", "The events each service has a standard call of its own for, which feed its built-in reports; unticked, the service gets none of it, and the game may log the service's own event by that name as one of its events.");
            if (!section.Open) return;

            var columns = Columns();
            var table   = DrawHeader(columns, false);
            foreach (AnalyticsStandardEvent standard in System.Enum.GetValues(typeof(AnalyticsStandardEvent))) {
                var row = SirenixEditorGUI.PrefixRect(EditorGUILayout.GetControlRect(), out var prefix);
                EditorGUI.LabelField(prefix, standardEventLabels.TryGetValue(standard, out var label) ? label : new GUIContent(ObjectNames.NicifyVariableName(standard.ToString())));
                for (var i = 0; i < columns.Count; i++) {
                    var name = AnalyticsEditor.services.FirstOrDefault(service => service.Id == columns[i].Id)?.StandardEventName(standard);
                    var tip  = columns[i].IsEditor ? "The Editor prints it to the Console." : name != null ? $"{columns[i].Title.text} gets it as {name}." : $"{columns[i].Title.text} gets it through its own call.";
                    if (!Tick(settings, table.Box(i, row), settings.Takes(columns[i].Id, standard), tip, out var next)) continue;

                    settings.SetTakes(columns[i].Id, standard, next);
                    EditorUtility.SetDirty(settings);
                }

                DrawFromPack(Rect.MinMaxRect(table.End, row.y, row.xMax, row.yMax), standard, root);
            }
        }

        private static readonly GUIContent nameHeader        = new("Name", "What the services get, and the event's member of AnalyticsEvent in code.");
        private static readonly GUIContent descriptionHeader = new("Description", "Shown on the event's member of AnalyticsEvent in code.");

        // Inset as a padded section's rows, so the header's columns line up with the events' rows below.
        private static GUIStyle headerStyle;

        // The events the game logs, one row each, added with the bar's +, with the Standard Events table's columns: a tick sends the row to
        // the service; an event added goes to every service until unticked.
        private static void DrawEventList(AnalyticsSettings settings, InspectorProperty events) {
            if (!restored) {
                Section.Restore(events);
                restored = true;
            }

            var columns  = Columns();
            var resolver = (IOrderedCollectionResolver) events.ChildResolver;
            using var section = Section.Foldout(events, "Events", "What the game logs with AnalyticsSdk.Log, as AnalyticsEvent members once applied, and the services each goes to; user data goes to every service.", buttons: 1, padded: false);
            if (section.Button(0, SdfIconType.Plus, "Add an event")) {
                resolver.QueueAdd(new object[] { new AnalyticsEventEntry { Id = AnalyticsEventsFile.NewId(settings), Name = UnusedName(settings) } });
                // The new row shows, on a folded section too.
                Section.SetExpandedRecursively(events, true);
            }

            if (!section.Open) return;

            headerStyle ??= new GUIStyle { padding = new RectOffset(Section.Inset, Section.Inset, (int) Section.RowPadding, (int) Section.RowPadding) };
            GUILayout.BeginVertical(headerStyle);
            var table = DrawHeader(columns, true, nameHeader, descriptionHeader);
            GUILayout.EndVertical();

            rows.Draw(events.Children.Count, i => DrawEvent(settings, events.Children[i], columns, table), "None", (from, to) => {
                // Odin's resolver has no move, so the event leaves its place and comes back at the other.
                var moved = events.Children[from].ValueEntry.WeakSmartValue;
                resolver.QueueRemoveAt(from);
                resolver.QueueInsertAt(to, new[] { moved });
            }, resolver.QueueRemoveAt, "Remove the event");
        }

        // Puts @ before a C# keyword, as the pack's source generator does.
        private static readonly Microsoft.CSharp.CSharpCodeProvider csharp = new();

        // An event on one line: its name in the label column, a tick under each column, its description after them, as narrow as they leave it.
        // The name and the description are written through the value entries, with undo; its id is for enum fields only.
        private static void DrawEvent(AnalyticsSettings settings, InspectorProperty property, List<Column> columns, Table table) {
            var name        = property.Children["name"];
            var description = property.Children["description"];
            var entry       = (AnalyticsEventEntry) property.ValueEntry.WeakSmartValue;

            // Placed by the item rather than the layout, as the header's columns are.
            var value     = SectionRows.MarkFirstRow(EditorGUILayout.GetControlRect(), out var prefix);
            // As wide as its text, at least as wide as most names need so that the list's names line up, and never closer than the inset to the
            // value column, as the IAP page's product ids.
            const float least = 120;
            var text      = name.ValueEntry.WeakSmartValue as string ?? "";
            var nameWidth = Mathf.Max(0, Mathf.Min(Mathf.Max(EditorStyles.textField.CalcSize(new GUIContent(text)).x + Section.Inset, least), value.x - Section.Inset - prefix.x));
            var nameField = new Rect(prefix.x, value.y, nameWidth, value.height);
            EditorGUI.BeginChangeCheck();
            var newName = EditorGUI.DelayedTextField(nameField, text);
            if (EditorGUI.EndChangeCheck()) name.ValueEntry.WeakSmartValue = newName.Trim();
            GUI.Label(nameField, new GUIContent("", $"What the services get; AnalyticsEvent.{csharp.CreateEscapedIdentifier(text)} in code"));
            SettingsFocus.Drawn((UnityEngine.Object) property.Tree.WeakTargets[0], name.UnityPropertyPath, nameField);

            for (var i = 0; i < columns.Count; i++) {
                var on = entry.Sends(columns[i].Id);
                if (!Tick(settings, table.Box(i, value), on, $"{(on ? "Goes" : "Does not go")} to {columns[i].Title.text}.", out var next)) continue;

                entry.SetSends(columns[i].Id, next);
                EditorUtility.SetDirty(settings);
            }

            var descriptionField = Rect.MinMaxRect(table.End, value.y, value.xMax, value.yMax);
            if (descriptionField.width <= 0) return;

            EditorGUI.BeginChangeCheck();
            var newDescription = EditorGUI.DelayedTextField(descriptionField, description.ValueEntry.WeakSmartValue as string ?? "");
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

        /// <summary>Everything the installed services lack, for the page and the build.</summary>
        internal static IEnumerable<SettingsIssue> Issues() {
            if (SettingsAssetUtils.OutsideResources<AnalyticsSettings>(AnalyticsSettings.ResourceName) is { } path) {
                yield return new SettingsIssue(BuildTarget.NoTarget, true, $"{path} is outside a Resources folder, so the game cannot load it and sends every event everywhere.", "Move it", () => SettingsAssetUtils.MoveToResources<AnalyticsSettings>(AnalyticsSettings.ResourceName));
            }

            if (Find() is { } settings) {
                foreach (var issue in AnalyticsEventRules.Issues(settings)) yield return issue;
            }

            foreach (var service in AnalyticsEditor.services) {
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