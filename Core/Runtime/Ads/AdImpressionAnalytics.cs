using RiseOn.Ads;
using UnityEngine;

namespace RiseOn.Analytics {
    /// <summary>Every ad impression's revenue goes to the analytics services, with no call from the game, unless the page turns it off.</summary>
    internal static class AdImpressionAnalytics {
        // After the ads pack's reset of its events at SubsystemRegistration.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Listen() {
            AdsSdk.Paid += impression => {
                if (AnalyticsSdk.Settings.AdRevenueFromAds) AnalyticsSdk.LogAdRevenue(ToAdRevenue(impression));
            };
        }

        private static AdRevenue ToAdRevenue(AdImpression impression) {
            return new(impression.Mediation, impression.Network, impression.Format.ToString(), impression.UnitId, impression.Value, impression.Currency);
        }
    }
}