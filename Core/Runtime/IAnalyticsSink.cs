using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using RiseOn.Consent;

namespace RiseOn.Analytics {
    /// <summary>
    /// One analytics service.<br/>
    /// Its package registers it with <see cref="AnalyticsSdk.Register"/> before the first scene; <see cref="AnalyticsSdk"/> starts it right away and hands it every call, on Unity's thread, only after it started.
    /// </summary>
    public interface IAnalyticsSink {
        /// <summary>The name the project settings know it by, e.g. firebase.</summary>
        string Id { get; }

        /// <summary>What this service would refuse or cut in the event; the Editor and Development Builds stop on it.</summary>
        IEnumerable<string> Check(string name, IReadOnlyList<AnalyticsParameter> parameters);

        /// <summary>
        /// Waits for what the service needs, the consent flow or another SDK, and starts it; false when it cannot run this session, and its calls are then dropped.
        /// </summary>
        UniTask<bool> StartAsync();

        /// <summary>The player changed their choices in the privacy options form, after the service started.</summary>
        void ApplyConsent(ConsentState consent);

        void Log(string name, IReadOnlyList<AnalyticsParameter> parameters);
        void LogAdRevenue(in AdRevenue revenue);
        void LogPurchase(in PurchaseRevenue purchase);
        void SetUserId(string id);
        void SetUserProperty(string name, string value);
    }
}