using System;
using System.Collections.Generic;
using RiseOn.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Analytics {
    /// <summary>
    /// The project's analytics settings, edited in Project Settings > RiseOn > Analytics and kept in a Resources asset: which services take ad revenue and purchases, the game's events, which of them each service takes, and whether the Editor prints what it would send.
    /// </summary>
    internal sealed class AnalyticsSettings : ScriptableObject {
        public const string ResourceName = "RiseOnAnalyticsSettings";

        /// <summary>The events one service takes; a service with no entry takes every event.</summary>
        [Serializable]
        public struct SinkEvents {
            [SerializeField]
            private string sink;

            [SerializeField]
            private bool onlyListed;

            [SerializeField, SectionList(Item = "event")]
            private List<AnalyticsEvent> events;

            public SinkEvents(string sink, bool onlyListed, List<AnalyticsEvent> events) {
                this.sink       = sink;
                this.onlyListed = onlyListed;
                this.events     = events;
            }

            public string Sink => sink ?? "";
            public bool OnlyListed => onlyListed;
            public IReadOnlyList<AnalyticsEvent> Events => events ?? (IReadOnlyList<AnalyticsEvent>)Array.Empty<AnalyticsEvent>();
        }

        [SerializeField, LabelText("Log in Editor"), Tooltip("Prints the events, ad revenue and purchases the Editor's column and row take to the Console in the Editor, which sends nothing to the services, as their SDKs are native; the services' own logs are their SDK Log.")]
        private bool logInEditor = true;

        [SerializeField, LabelText("Ad Revenue from Ads"), Tooltip("Sends the revenue of every ad com.riseon.ads shows to the services the Revenue table ticks; off for a game that calls AnalyticsSdk.LogAdRevenue itself.")]
        private bool adRevenueFromAds = true;

        [SerializeField, LabelText("Purchases from IAP"), Tooltip("Sends every purchase com.riseon.iap confirms to the services the Revenue table ticks; off for a game that calls AnalyticsSdk.LogPurchase itself.")]
        private bool purchasesFromIap = true;

        // The services, by id, that the Revenue table leaves out of each row: every service takes both until unticked.
        [SerializeField]
        private List<string> adRevenueOff = new();

        [SerializeField]
        private List<string> purchaseOff = new();

        [SerializeField]
        private List<AnalyticsEventEntry> events = new();

        [SerializeField]
        private List<SinkEvents> sinkEvents = new();

        public bool LogInEditor => logInEditor;

        /// <summary>Whether the ad revenue com.riseon.ads reports goes to the services with no call from the game.</summary>
        public bool AdRevenueFromAds => adRevenueFromAds;

        /// <summary>Whether the purchases com.riseon.iap confirms go to the services with no call from the game.</summary>
        public bool PurchasesFromIap => purchasesFromIap;
        public IReadOnlyList<SinkEvents> Sinks => sinkEvents;

        /// <summary>The game's events, which the pack's source generator compiles into <see cref="AnalyticsEvent"/> once the page applies them.</summary>
        public List<AnalyticsEventEntry> Events => events;

        /// <summary>Makes the service take only its listed events, or every event; its first change gives it an entry, with an empty list.</summary>
        internal void SetOnlyListed(string sink, bool onlyListed) {
            var index = sinkEvents.FindIndex(entry => entry.Sink == sink);
            if (index < 0) sinkEvents.Add(new SinkEvents(sink, onlyListed, new List<AnalyticsEvent>()));
            else sinkEvents[index] = new SinkEvents(sink, onlyListed, new List<AnalyticsEvent>(sinkEvents[index].Events));
        }

        /// <summary>Whether the service takes this revenue: its standard call for it, as the Revenue table ticks.</summary>
        public bool Takes(string sink, AnalyticsRevenue revenue) => !Off(revenue).Contains(sink);

        /// <summary>Ticks or unticks the service in the revenue's row.</summary>
        internal void SetTakes(string sink, AnalyticsRevenue revenue, bool on) {
            var off = Off(revenue);
            off.RemoveAll(id => id == sink);
            if (!on) off.Add(sink);
        }

        private List<string> Off(AnalyticsRevenue revenue) => revenue is AnalyticsRevenue.Ad ? adRevenueOff ??= new List<string>() : purchaseOff ??= new List<string>();

        /// <summary>Whether the service takes this event; user data always goes.</summary>
        public bool Sends(string sink, AnalyticsEvent @event) {
            foreach (var entry in sinkEvents) {
                if (entry.Sink != sink) continue;
                if (!entry.OnlyListed) return true;
                foreach (var listed in entry.Events) {
                    if (listed == @event) return true;
                }

                return false;
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