using System.Collections.Generic;
using System.Text.RegularExpressions;
using RiseOn.Analytics.Editor;
using RiseOn.Utils.Editor;
using UnityEditor;

namespace RiseOn.Analytics.AppsFlyer.Editor {
    /// <summary>AppsFlyer's section of Project Settings > RiseOn > Analytics: its keys, and a build stop when one is missing.</summary>
    internal static class AppsFlyerAnalyticsEditor {
        private static readonly Regex appleAppId = new(@"^\d+$");

        private static readonly SettingsTree tree = new();

        [InitializeOnLoadMethod]
        private static void Register() {
            AnalyticsEditor.RegisterService("appsflyer", "AppsFlyer", draw: () => tree.Draw(SettingsAssetUtils.LoadOrCreate<AppsFlyerSettings>(AppsFlyerSettings.ResourceName)), issues: Issues,
                                            standardEvents: new Dictionary<AnalyticsStandardEvent, string> { { AnalyticsStandardEvent.Purchase, "af_purchase" } });
        }

        private static IEnumerable<SettingsIssue> Issues() {
            if (SettingsAssetUtils.OutsideResources<AppsFlyerSettings>(AppsFlyerSettings.ResourceName) is { } path) {
                yield return new SettingsIssue(BuildTarget.NoTarget, true, $"{path} is outside a Resources folder, so the game cannot load AppsFlyer's keys.", "Move it", () => SettingsAssetUtils.MoveToResources<AppsFlyerSettings>(AppsFlyerSettings.ResourceName));
            }

            var settings = SettingsAssetUtils.Find<AppsFlyerSettings>(AppsFlyerSettings.ResourceName);
            if (!settings || settings.DevKey.Length == 0) {
                yield return new SettingsIssue(BuildTarget.NoTarget, true, "AppsFlyer's Dev Key is empty: copy it from the AppsFlyer dashboard's App settings into the AppsFlyer section.",
                                                fieldAsset: settings, fieldPath: "devKey");
            }

            var appId = settings ? settings.AppleAppId : "";
            if (!appleAppId.IsMatch(appId)) {
                yield return new SettingsIssue(BuildTarget.iOS, true, fieldAsset: settings, fieldPath: "appleAppId", message: appId.Length == 0 ? "AppsFlyer's App Store ID is empty: the digits of the game's App Store link, without \"id\"." : $"AppsFlyer's App Store ID \"{appId}\" is not one, which is only digits.");
            }

#if RISEON_EDM_SPM
            // Through Swift Package Manager, AppsFlyer 7's framework is linked dynamically but not embedded.
            if (Google.IOSResolver.SwiftPackageManagerEnabled) {
                yield return new SettingsIssue(BuildTarget.iOS, true, "External Dependency Manager resolves the iOS libraries through Swift Package Manager, which links AppsFlyer's framework without embedding it, so the game crashes at launch; resolve them through CocoaPods.",
                                                "Use CocoaPods", () => Google.IOSResolver.SwiftPackageManagerEnabled = false);
            }
#endif
        }
    }

    [CustomEditor(typeof(AppsFlyerSettings))]
    internal sealed class AppsFlyerSettingsInspector : SettingsAssetInspector {
        protected override string PagePath => "Project/RiseOn/Analytics";
    }
}