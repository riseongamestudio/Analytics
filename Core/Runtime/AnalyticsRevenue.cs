namespace RiseOn.Analytics {
    /// <summary>The revenue each service has a standard call for, a row of the page's Revenue table.</summary>
    internal enum AnalyticsRevenue {
        /// <summary>An ad's revenue: <see cref="AnalyticsSdk.LogAdRevenue"/>.</summary>
        Ad,

        /// <summary>A store purchase: <see cref="AnalyticsSdk.LogPurchase"/>.</summary>
        Purchase
    }
}