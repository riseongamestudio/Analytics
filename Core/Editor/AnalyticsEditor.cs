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

            /// <summary>The event name of its standard ad revenue call, and of its purchase call; null for a call that is no event.</summary>
            public readonly string AdRevenueEvent;
            public readonly string PurchaseEvent;

            public Service(string id, string title, string tooltip, Action draw, Func<IEnumerable<SettingsIssue>> issues, string package, Func<string, IEnumerable<string>> eventProblems,
                           string adRevenueEvent, string purchaseEvent) {
                Id      = id;
                Title   = title;
                Tooltip = tooltip;
                Draw    = draw;
                Issues  = issues;
                Package = package;
                EventProblems = eventProblems;
                AdRevenueEvent = adRevenueEvent;
                PurchaseEvent  = purchaseEvent;
            }

            public string RevenueEvent(AnalyticsRevenue revenue) => revenue is AnalyticsRevenue.Ad ? AdRevenueEvent : PurchaseEvent;
        }

        internal static readonly List<Service> services = new();

        /// <param name="id">The service's <see cref="IAnalyticsSink.Id"/>.</param>
        /// <param name="tooltip">What the section's header explains on hover; null for nothing.</param>
        /// <param name="draw">Its own fields, drawn under its events; null for none.</param>
        /// <param name="issues">What its setup lacks; null for nothing to check.</param>
        /// <param name="eventProblems">Why it would refuse an event of the given name, as the page checks each event; null for no rule of its own.</param>
        /// <param name="adRevenueEvent">The event its <see cref="IAnalyticsSink.LogAdRevenue"/> sends, such as ad_impression; null for a call that is no event. An event of the game's by that name, to a service that takes ad revenue, would count it twice.</param>
        /// <param name="purchaseEvent">As <paramref name="adRevenueEvent"/>, for <see cref="IAnalyticsSink.LogPurchase"/>.</param>
        // Not inlined, so the calling assembly is the service's own.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RegisterService(string id, string title, string tooltip = null, Action draw = null, Func<IEnumerable<SettingsIssue>> issues = null, Func<string, IEnumerable<string>> eventProblems = null,
                                           string adRevenueEvent = null, string purchaseEvent = null) {
            var package = PackageInfo.FindForAssembly(Assembly.GetCallingAssembly())?.name;
            services.RemoveAll(service => service.Id == id);
            services.Add(new Service(id, title, tooltip, draw, issues, package, eventProblems, adRevenueEvent, purchaseEvent));
            services.Sort((a, b) => string.CompareOrdinal(a.Title, b.Title));
        }
    }
}