using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using RiseOn.Utils.Editor;
using UnityEditor.PackageManager;

namespace RiseOn.Analytics.Editor {
    /// <summary>
    /// What a service's Editor assembly adds to Project Settings > RiseOn > Analytics: its section under the events it takes, and what its setup lacks.<br/>
    /// Register from an [InitializeOnLoadMethod].
    /// </summary>
    public static class AnalyticsEditor {
        internal sealed class Service {
            public readonly string                             Id;
            public readonly string                             Title;
            public readonly string                             Tooltip;
            public readonly Action                             Draw;
            public readonly Func<IEnumerable<SettingsIssue>> Issues;

            /// <summary>The package of the assembly that registered it; null for one the game's own code registered.</summary>
            public readonly string Package;

            public readonly Func<string, IEnumerable<string>> EventProblems;

            public Service(string id, string title, string tooltip, Action draw, Func<IEnumerable<SettingsIssue>> issues, string package, Func<string, IEnumerable<string>> eventProblems) {
                Id      = id;
                Title   = title;
                Tooltip = tooltip;
                Draw    = draw;
                Issues  = issues;
                Package = package;
                EventProblems = eventProblems;
            }
        }

        internal static readonly List<Service> Services = new();

        /// <param name="id">The service's <see cref="IAnalyticsSink.Id"/>.</param>
        /// <param name="tooltip">What the section's header explains on hover; null for nothing.</param>
        /// <param name="draw">Its own fields, drawn under its events; null for none.</param>
        /// <param name="issues">What its setup lacks; null for nothing to check.</param>
        /// <param name="eventProblems">Why it would refuse an event of the given name, as the page checks each event; null for no rule of its own.</param>
        // Not inlined, so the calling assembly is the service's own.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RegisterService(string id, string title, string tooltip = null, Action draw = null, Func<IEnumerable<SettingsIssue>> issues = null, Func<string, IEnumerable<string>> eventProblems = null) {
            var package = PackageInfo.FindForAssembly(Assembly.GetCallingAssembly())?.name;
            Services.RemoveAll(service => service.Id == id);
            Services.Add(new Service(id, title, tooltip, draw, issues, package, eventProblems));
            Services.Sort((a, b) => string.CompareOrdinal(a.Title, b.Title));
        }
    }
}