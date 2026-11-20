using RiseOn.Iap;
using UnityEngine;

namespace RiseOn.Analytics {
    /// <summary>Every purchase the store confirms goes to the analytics services once, with no call from the game, unless the page turns it off.</summary>
    internal static class IapAnalytics {
        // After the IAP pack's reset of its events at SubsystemRegistration.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Listen() {
            IapSdk.Purchased += purchase => {
                if (AnalyticsSdk.Settings.PurchasesFromIap) AnalyticsSdk.LogPurchase(new PurchaseRevenue(purchase.ProductId, purchase.Value, purchase.Currency, purchase.OrderId, purchase.Quantity));
            };
        }
    }
}