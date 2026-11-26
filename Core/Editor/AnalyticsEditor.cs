using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using RiseOn.Utils.Editor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace RiseOn.Analytics.Editor {
    /// <summary>
    /// What a service's Editor assembly adds to Project Settings > RiseOn > Analytics: its row of the Services section with its fields, its column of the events tables, and what its setup lacks.<br/>
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

            /// <summary>Its column's header in the events tables; null for its title.</summary>
            public readonly Func<Texture> Icon;

            private readonly IReadOnlyDictionary<AnalyticsStandardEvent, string> standardEvents;

            public Service(string id, string title, string tooltip, Action draw, Func<IEnumerable<SettingsIssue>> issues, string package, Func<string, IEnumerable<string>> eventProblems,
                           IReadOnlyDictionary<AnalyticsStandardEvent, string> standardEvents, Func<Texture> icon) {
                Id      = id;
                Title   = title;
                Tooltip = tooltip;
                Draw    = draw;
                Issues  = issues;
                Package = package;
                EventProblems = eventProblems;
                Icon          = icon;
                this.standardEvents = standardEvents;
            }

            /// <summary>The name of the event its call for the standard event sends; null for a call that is no event.</summary>
            public string StandardEventName(AnalyticsStandardEvent standardEvent) => standardEvents != null && standardEvents.TryGetValue(standardEvent, out var name) ? name : null;
        }

        internal static readonly List<Service> services = new();

        /// <param name="id">The service's <see cref="IAnalyticsSink.Id"/>.</param>
        /// <param name="tooltip">What the section's header explains on hover; null for nothing.</param>
        /// <param name="draw">Its own fields, folded under its row of the Services section; null for none, which leaves the row its name alone.</param>
        /// <param name="issues">What its setup lacks; null for nothing to check.</param>
        /// <param name="eventProblems">Why it would refuse an event of the given name, as the page checks each event; null for no rule of its own.</param>
        /// <param name="standardEvents">The name of the event each of its standard calls sends, such as ad_impression for <see cref="AnalyticsStandardEvent.AdRevenue"/>; a call that sends no event of a name has no entry. An event of the game's by that name, to a service that takes the standard event too, would count twice.</param>
        /// <param name="icon">Its column's header in the events tables, called as the page draws, one line of text tall, in the Editor skin's colors; its title hovers over it. Null, or returning null, for its title instead.</param>
        // Not inlined, so the calling assembly is the service's own.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RegisterService(string id, string title, string tooltip = null, Action draw = null, Func<IEnumerable<SettingsIssue>> issues = null, Func<string, IEnumerable<string>> eventProblems = null,
                                           IReadOnlyDictionary<AnalyticsStandardEvent, string> standardEvents = null, Func<Texture> icon = null) {
            var package = PackageInfo.FindForAssembly(Assembly.GetCallingAssembly())?.name;
            services.RemoveAll(service => service.Id == id);
            services.Add(new Service(id, title, tooltip, draw, issues, package, eventProblems, standardEvents, icon));
            services.Sort((a, b) => string.CompareOrdinal(a.Title, b.Title));
        }
    }
}