using System.Collections.Generic;
using RiseOn.Analytics.Editor;
using RiseOn.Utils.Editor;
using UnityEditor;

namespace RiseOn.Analytics.Firebase.Editor {
    /// <summary>Firebase Analytics' section of Project Settings > RiseOn > Analytics: whether Google Play purchases reach it on their own, and the names its rules refuse.</summary>
    internal static class FirebaseAnalyticsEditor {
        private static readonly SettingsTree tree = new();

        [InitializeOnLoadMethod]
        private static void Register() {
            AnalyticsEditor.RegisterService(FirebaseAnalyticsSink.SinkId, "Firebase Analytics",
                "Where EU rules apply, the consent form sets Analytics' consent mode once consent mode is on in AdMob > Privacy & messaging.",
                () => tree.Draw(SettingsAssetUtils.LoadOrCreate<FirebaseAnalyticsSettings>(FirebaseAnalyticsSettings.ResourceName)), Issues, FirebaseAnalyticsSink.EventNameProblems,
                new Dictionary<AnalyticsStandardEvent, string> { { AnalyticsStandardEvent.AdRevenue, "ad_impression" }, { AnalyticsStandardEvent.Purchase, "in_app_purchase" } },
                // Firebase's logo, from its site.
                () => AssetDatabase.LoadAssetAtPath<UnityEngine.Texture>("Packages/com.riseon.analytics.firebase/Editor/Firebase.png"));
        }

        private static IEnumerable<SettingsIssue> Issues() {
            if (SettingsAssetUtils.OutsideResources<FirebaseAnalyticsSettings>(FirebaseAnalyticsSettings.ResourceName) is { } path) {
                yield return new SettingsIssue(BuildTarget.NoTarget, true, $"{path} is outside a Resources folder, so the game cannot load Firebase Analytics' settings.", "Move it",
                                                () => SettingsAssetUtils.MoveToResources<FirebaseAnalyticsSettings>(FirebaseAnalyticsSettings.ResourceName));
            }
        }
    }

    [CustomEditor(typeof(FirebaseAnalyticsSettings))]
    internal sealed class FirebaseAnalyticsSettingsInspector : SettingsAssetInspector {
        protected override string PagePath => "Project/RiseOn/Analytics";
    }
}