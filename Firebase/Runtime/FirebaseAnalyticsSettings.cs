using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Analytics.Firebase {
    /// <summary>Firebase Analytics' own settings for this game, edited in Project Settings > RiseOn > Analytics and kept in a Resources asset the build carries.</summary>
    internal sealed class FirebaseAnalyticsSettings : ScriptableObject {
        public const string ResourceName = "RiseOnFirebaseAnalyticsSettings";

        [SerializeField, LabelText("Google Play Linked")]
        [Tooltip("The Firebase project is linked to Google Play (Firebase console > Project settings > Integrations), so Analytics collects Google Play purchases itself and the pack sends none on Android, which would count them twice; off, the pack logs in_app_purchase for each.")]
        private bool googlePlayLinked;

        /// <summary>Analytics collects Google Play purchases itself, so the pack logs none on Android.</summary>
        public bool GooglePlayLinked => googlePlayLinked;

        public static FirebaseAnalyticsSettings Load() {
            var asset = Resources.Load<FirebaseAnalyticsSettings>(ResourceName);
            return asset ? asset : CreateInstance<FirebaseAnalyticsSettings>();
        }
    }
}