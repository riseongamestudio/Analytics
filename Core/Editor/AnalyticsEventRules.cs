using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RiseOn.Utils.Editor;
using UnityEditor;

namespace RiseOn.Analytics.Editor {
    /// <summary>What stops the events from compiling into <see cref="AnalyticsEvent"/> or from reaching a service, each with the name to edit.</summary>
    internal static class AnalyticsEventRules {
        // A member the generator can write, in ASCII, as every service's narrower rule is.
        private static readonly Regex Member = new("^[A-Za-z_][A-Za-z0-9_]*$");

        public static IEnumerable<SettingsIssue> Issues(AnalyticsSettings settings) {
            var events = settings.Events;
            for (var i = 0; i < events.Count; i++) {
                var name = events[i].Name ?? "";
                var path = Path(i);
                if (!Member.IsMatch(name)) {
                    yield return Issue(settings, $"\"{name}\" is not an event name: a letter or underscore, then letters, digits or underscores.", path);
                    continue;
                }

                if (name == "value__") yield return Issue(settings, "C# keeps value__ for itself in every enum, so no event can be value__.", path);

                // Each installed service's own rules, as it would refuse the event at run time.
                foreach (var service in AnalyticsEditor.Services.Where(service => service.EventProblems != null)) {
                    foreach (var problem in service.EventProblems(name)) yield return Issue(settings, $"{service.Title}: {problem}.", path);
                }
            }

            foreach (var group in events.Select((entry, index) => (entry.Name, Index: index)).GroupBy(entry => entry.Name).Where(group => group.Count() > 1)) {
                yield return Issue(settings, $"The event {group.Key} is listed {group.Count()} times.", Path(group.Last().Index));
            }

            // NewId never repeats an id of the list it adds to, so only two branches meet here. Each id may be saved in enum fields on its
            // branch, so only a person can tell which event keeps it.
            foreach (var group in events.Select((entry, index) => (entry.Id, entry.Name, Index: index)).Where(entry => entry.Id > 0).GroupBy(entry => entry.Id).Where(group => group.Count() > 1)) {
                yield return Issue(settings, $"Events {string.Join(", ", group.Select(entry => entry.Name))} share the id {group.Key}. Remove the one serialized enum fields do not mean and add it again for a new id.", Path(group.Last().Index));
            }
        }

        private static string Path(int index) => $"events.Array.data[{index}].name";

        private static SettingsIssue Issue(AnalyticsSettings settings, string message, string path) => new(BuildTarget.NoTarget, true, message, fieldAsset: settings, fieldPath: path);
    }
}