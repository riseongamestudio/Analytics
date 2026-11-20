using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Analytics.AppsFlyer {
    /// <summary>
    /// AppsFlyer's keys for this game, edited in Project Settings > RiseOn > Analytics and kept in a Resources asset the build carries: the dev key is in every app that uses AppsFlyer, so it is no secret.
    /// </summary>
    internal sealed class AppsFlyerSettings : ScriptableObject {
        public const string ResourceName = "RiseOnAppsFlyerSettings";

        [SerializeField, Delayed, LabelText("Dev Key"), Tooltip("AppsFlyer > App settings > Dev key; one for both platforms.")]
        private string devKey;

        [SerializeField, Delayed, LabelText("App Store ID"), Tooltip("The numbers of the App Store link, without \"id\"; iOS only.")]
        private string appleAppId;

        [SerializeField, LabelText("SDK Log"), Tooltip("AppsFlyer's own log, in Development Builds only.")]
        private bool debugLog;

        /// <summary>AppsFlyer > App settings > Dev key, the same for both platforms.</summary>
        public string DevKey => devKey?.Trim() ?? "";

        /// <summary>The App Store's numeric app id, without "id".</summary>
        public string AppleAppId => appleAppId?.Trim() ?? "";

        /// <summary>The SDK's own log, in Development Builds only.</summary>
        public bool DebugLog => debugLog;

        public static AppsFlyerSettings Load() {
            var asset = Resources.Load<AppsFlyerSettings>(ResourceName);
            return asset ? asset : CreateInstance<AppsFlyerSettings>();
        }
    }
}