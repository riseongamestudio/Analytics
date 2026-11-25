namespace RiseOn.Analytics {
    /// <summary>
    /// An event each service has a standard call of its own for, which feeds its built-in reports: a row of the Standard Events table of Project Settings > RiseOn > Analytics, which ticks the services that take it.<br/>
    /// A new kind of standard event is a new member, a new row and a new call on <see cref="AnalyticsSdk"/> and <see cref="IAnalyticsSink"/>.
    /// </summary>
    public enum AnalyticsStandardEvent {
        /// <summary>An ad's revenue: <see cref="AnalyticsSdk.LogAdRevenue"/>.</summary>
        AdRevenue,

        /// <summary>A store purchase: <see cref="AnalyticsSdk.LogPurchase"/>.</summary>
        Purchase
    }
}