using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RiseOn.Utils.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Analytics.Editor {
    /// <summary>
    /// The events as the pack's source generator reads them, one "id, name, description" line each, split by tabs; it compiles them into
    /// RiseOn.Analytics as <see cref="AnalyticsEvent"/>.<br/>
    /// Written when Apply is pressed or chosen as the settings page closes, and when the asset is imported from elsewhere (a pull, a merge) while
    /// the page is closed; never by hand. Apply renames the members the project's scripts name too, so they keep compiling.
    /// </summary>
    internal sealed class AnalyticsEventsFile : AssetPostprocessor {
        // Unity hands a project's *.{generator assembly}.additionalfile to that generator.
        internal const string FilePath = "Assets/RiseOn/Analytics/Events.RiseOnAnalyticsGenerator.additionalfile";

        /// <summary>While the settings page draws: it saves the asset on every edit, and writes the file only when Apply is pressed or chosen as it closes.</summary>
        internal static bool Held;

        // The events as they were the last time the page saw them match the file, for Revert. In Library, as only this machine kept them.
        private const string AppliedPath = "Library/RiseOn/AnalyticsEventsApplied.json";

        private static string  appliedJson;
        private static Applied applied;

        [Serializable]
        private sealed class Applied {
            public string                    file;
            public List<AnalyticsEventEntry> events;
        }

        /// <summary>The file says what the events say.</summary>
        internal static bool IsCurrent(AnalyticsSettings settings) => FileText() == Text(settings);

        /// <summary>The asset's events are not the file's, and nothing stops the file from being written.</summary>
        internal static bool IsPending(AnalyticsSettings settings) => !AnalyticsEventRules.Issues(settings).Any() && !IsCurrent(settings);

        /// <summary>The events whose name changed since the file was written, by id, as code names them.</summary>
        internal static List<(string From, string To)> Renames(AnalyticsSettings settings) {
            var written = new Dictionary<int, string>();
            foreach (var line in FileText().Split('\n')) {
                var parts = line.Split('\t');
                if (parts.Length == 3 && int.TryParse(parts[0], out var id)) written[id] = parts[1];
            }

            return settings.Events.Where(entry => written.TryGetValue(entry.Id, out var name) && name != entry.Name).Select(entry => (written[entry.Id], entry.Name)).ToList();
        }

        /// <summary>
        /// Writes the file when its text would change and the events have no problem, renaming the members the project's scripts name first;
        /// Unity recompiles after.
        /// </summary>
        internal static void Sync(AnalyticsSettings settings) {
            Number(settings);
            if (!IsPending(settings)) return;

            var renamed = CodeRename.Members(nameof(AnalyticsEvent), Renames(settings));
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, Text(settings), new UTF8Encoding(false));
            Remember(settings);
            // One refresh, so the renamed scripts and the new members compile together.
            AssetDatabase.Refresh();
            if (renamed.Count > 0) Debug.Log($"Analytics: renamed events in {string.Join(", ", renamed)}");
        }

        /// <summary>Keeps the events while the file matches them, so <see cref="Revert"/> can come back to them; the page calls it as it draws.</summary>
        internal static void Remember(AnalyticsSettings settings) {
            var json = JsonUtility.ToJson(new Applied { file = Text(settings), events = settings.Events });
            if (json == LoadAppliedJson()) return;

            Directory.CreateDirectory(Path.GetDirectoryName(AppliedPath)!);
            File.WriteAllText(AppliedPath, json, new UTF8Encoding(false));
            appliedJson = json;
            applied     = null;
        }

        /// <summary>Why <see cref="Revert"/> cannot run, as a button's tooltip; null when it can.</summary>
        internal static string RevertProblem() {
            var last = LoadApplied();
            if (last == null) return "Revert is off.\nThis machine kept no events from the last time they matched the events file.";
            if (last.file != FileText()) return "Revert is off.\nThe events file changed elsewhere (a pull, a merge) after this machine last wrote it.";
            return null;
        }

        /// <summary>Takes the events back to when the file last matched them, with undo; check <see cref="RevertProblem"/> first.</summary>
        internal static void Revert(AnalyticsSettings settings) {
            var last = LoadApplied();
            Undo.RecordObject(settings, "Revert Analytics Events");
            settings.Events.Clear();
            settings.Events.AddRange(last.events);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
        }

        private static string FileText() => File.Exists(FilePath) ? File.ReadAllText(FilePath) : "";

        private static string LoadAppliedJson() {
            appliedJson ??= File.Exists(AppliedPath) ? File.ReadAllText(AppliedPath) : "";
            return appliedJson;
        }

        private static Applied LoadApplied() {
            var json = LoadAppliedJson();
            if (json.Length == 0) return null;

            try {
                applied ??= JsonUtility.FromJson<Applied>(json);
            } catch (ArgumentException) {
                // A file cut short reads as none.
                return null;
            }

            return applied;
        }

        /// <summary>
        /// A random positive id no event of <paramref name="settings"/> has.<br/>
        /// Random, as Localization's DistributedUIDGenerator keeps key ids apart across machines, so events that two branches add do not meet on
        /// one id; 31 bits, as Unity serializes enums of 32 bits or smaller.
        /// </summary>
        internal static int NewId(AnalyticsSettings settings) {
            while (true) {
                var id = BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0) & int.MaxValue;
                if (id > 0 && settings.Events.All(entry => entry.Id != id)) return id;
            }
        }

        /// <summary>Gives an id to the events a hand edit left without one; the page gives the ones its + button adds.</summary>
        private static void Number(AnalyticsSettings settings) {
            var asset  = new SerializedObject(settings);
            var events = asset.FindProperty("events");
            for (var i = 0; i < events.arraySize; ++i) {
                var id = events.GetArrayElementAtIndex(i).FindPropertyRelative("id");
                if (id.intValue > 0) continue;

                id.intValue = NewId(settings);
                asset.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssetIfDirty(settings);
        }

        private static string Text(AnalyticsSettings settings) {
            var text = new StringBuilder();
            foreach (var entry in settings.Events.OrderBy(entry => entry.Id)) {
                var description = new string((entry.Description ?? "").Select(c => char.IsControl(c) ? ' ' : c).ToArray()).Trim();
                text.Append($"{entry.Id}\t{entry.Name}\t{description}\n");
            }

            return text.ToString();
        }

        // A pull or a merge changed the settings, or someone deleted the file.
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom) {
            if (Held) return;
            if (!imported.Any(path => Path.GetFileNameWithoutExtension(path) is AnalyticsSettings.ResourceName) && !deleted.Contains(FilePath)) return;

            var settings = SettingsAssetUtils.Find<AnalyticsSettings>(AnalyticsSettings.ResourceName);
            if (settings) Sync(settings);
        }
    }
}