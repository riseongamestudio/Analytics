using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Analytics {
    /// <summary>
    /// The project's analytics settings, edited in Project Settings > RiseOn > Analytics and kept in a Resources asset: which services take each standard event, and the game's events with the services each goes to.
    /// </summary>
    internal sealed class AnalyticsSettings : ScriptableObject {
        public const string ResourceName = "RiseOnAnalyticsSettings";

        [SerializeField, LabelText("Link RiseOn Ads"), Tooltip("com.riseon.ads sends the revenue of every ad it shows to the services ticked in this row; off for a game that calls AnalyticsSdk.LogAdRevenue itself.")]
        private bool adRevenueFromAds = true;

        [SerializeField, LabelText("Link RiseOn Iap"), Tooltip("com.riseon.iap sends every purchase it confirms to the services ticked in this row; off for a game that calls AnalyticsSdk.LogPurchase itself.")]
        private bool purchasesFromIap = true;

        // The ticks the Standard Events table took off, a service and a standard event each: every service takes every standard event
        // until unticked, a kind added later included.
        [SerializeField]
        private List<StandardEventOff> standardEventsOff = new();

        [Serializable]
        private struct StandardEventOff {
            public AnalyticsStandardEvent standardEvent;
            public string sink;
        }

        [SerializeField]
        private List<AnalyticsEventEntry> events = new();


        /// <summary>Whether the ad revenue com.riseon.ads reports goes to the services with no call from the game.</summary>
        public bool AdRevenueFromAds => adRevenueFromAds;

        /// <summary>Whether the purchases com.riseon.iap confirms go to the services with no call from the game.</summary>
        public bool PurchasesFromIap => purchasesFromIap;

        /// <summary>The game's events, which the pack's source generator compiles into <see cref="AnalyticsEvent"/> once the page applies them.</summary>
        public List<AnalyticsEventEntry> Events => events;

        /// <summary>Whether the service takes the standard event through its standard call, as the Standard Events table ticks.</summary>
        public bool Takes(string sink, AnalyticsStandardEvent standardEvent) {
            if (standardEventsOff == null) return true;

            foreach (var off in standardEventsOff) {
                if (off.standardEvent == standardEvent && off.sink == sink) return false;
            }

            return true;
        }

        /// <summary>Ticks or unticks the service in the standard event's row.</summary>
        internal void SetTakes(string sink, AnalyticsStandardEvent standardEvent, bool on) {
            standardEventsOff ??= new List<StandardEventOff>();
            standardEventsOff.RemoveAll(off => off.standardEvent == standardEvent && off.sink == sink);
            if (!on) standardEventsOff.Add(new StandardEventOff { standardEvent = standardEvent, sink = sink });
        }

        /// <summary>Whether the service takes this event; user data always goes.</summary>
        public bool Sends(string sink, AnalyticsEvent @event) {
            foreach (var entry in events) {
                if (entry.Id == (int) @event) return entry.Sends(sink);
            }

            return true;
        }

        /// <summary>The project's asset, or the defaults when the project never saved one.</summary>
        public static AnalyticsSettings Load() {
            var asset = Resources.Load<AnalyticsSettings>(ResourceName);
            return asset ? asset : CreateInstance<AnalyticsSettings>();
        }
    }
}