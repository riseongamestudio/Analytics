using RiseOn.Analytics.Editor;
using UnityEditor;

namespace RiseOn.Analytics.Firebase.Editor {
    /// <summary>Firebase's section of Project Settings > RiseOn > Analytics: the two settings that live outside Unity.</summary>
    internal static class FirebaseAnalyticsEditor {
        [InitializeOnLoadMethod]
        private static void Register() {
            AnalyticsEditor.RegisterService("firebase", "Google Analytics",
                "Google Play purchases reach Analytics once the Firebase project is linked to Google Play (Firebase console > Project settings > Integrations); App Store purchases go through AnalyticsSdk.LogPurchase. Where EU rules apply, the consent form sets Analytics' consent mode once consent mode is on in AdMob > Privacy & messaging.",
                eventProblems: FirebaseAnalyticsSink.EventNameProblems);
        }
    }
}