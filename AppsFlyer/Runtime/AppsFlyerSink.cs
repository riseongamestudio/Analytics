using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AppsFlyerSDK;
using Cysharp.Threading.Tasks;
using RiseOn.Consent;
using UnityEngine;
using SDK = AppsFlyerSDK.AppsFlyer;

namespace RiseOn.Analytics.AppsFlyer {
    /// <summary>
    /// AppsFlyer, started after the consent flow: where EU rules apply the SDK reads the TCF data the consent form wrote, and on iOS the player has answered the tracking prompt by then, so the first session carries both.
    /// </summary>
    internal sealed class AppsFlyerSink : IAnalyticsSink {
        public const string SinkId = "appsflyer";

        public string Id => SinkId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() {
            AnalyticsSdk.Register(new AppsFlyerSink());
        }

        public IEnumerable<string> Check(string name, IReadOnlyList<AnalyticsParameter> parameters) => Enumerable.Empty<string>();

        public async UniTask<bool> StartAsync() {
            var consent = await ConsentFlow.WaitUntilCompleteAsync();

            // Attribution identifies the device for ad networks, which a game for children must not do.
            if (consent.ChildDirected) {
                Debug.LogWarning($"{nameof(AppsFlyerSink)}: the game is for children, AppsFlyer does not start");
                return false;
            }

            var settings = AppsFlyerSettings.Load();
            var ios      = Application.platform is RuntimePlatform.IPhonePlayer;
            if (settings.DevKey.Length == 0 || (ios && settings.AppleAppId.Length == 0)) {
                Debug.LogError($"{nameof(AppsFlyerSink)}: Project Settings > RiseOn > Analytics lacks AppsFlyer's dev key{(ios ? " or App Store id" : "")}");
                return false;
            }

            await SDK.init(settings.DevKey, ios ? settings.AppleAppId : null);
            if (!SDK.isRPCBridgeAvailable()) {
                Debug.LogError($"{nameof(AppsFlyerSink)}: AppsFlyer's native bridge is missing from the build");
                return false;
            }

            if (settings.DebugLog && Debug.isDebugBuild) await SDK.enableDebug(true);
            await SDK.enableTCFDataCollection(true);
            await SDK.start();
            return true;
        }

        public void ApplyConsent(ConsentState consent) { }

        public void Log(string name, IReadOnlyList<AnalyticsParameter> parameters) {
            Fire(SDK.logEvent(name, parameters.ToDictionary(parameter => parameter.Name, parameter => parameter.ValueText)));
        }

        public void LogAdRevenue(in AdRevenue revenue) {
            var data       = new AFAdRevenueData(revenue.Network, Mediation(revenue.Mediation), revenue.Currency, revenue.Value);
            var additional = new Dictionary<string, string> { { AdRevenueScheme.AD_UNIT, revenue.UnitId }, { AdRevenueScheme.AD_TYPE, revenue.Format } };
            if (!string.IsNullOrEmpty(revenue.Placement)) additional[AdRevenueScheme.PLACEMENT] = revenue.Placement;
            Fire(SDK.logAdRevenue(data, additional));
        }

        public void LogPurchase(in PurchaseRevenue purchase) {
            Fire(SDK.logEvent(AFInAppEvents.PURCHASE, new Dictionary<string, string> {
                { AFInAppEvents.REVENUE, purchase.Value.ToString(CultureInfo.InvariantCulture) }
              , { AFInAppEvents.CURRENCY, purchase.Currency }
              , { AFInAppEvents.QUANTITY, purchase.Quantity.ToString(CultureInfo.InvariantCulture) }
              , { AFInAppEvents.CONTENT_ID, purchase.ProductId }
              , { AFInAppEvents.ORDER_ID, purchase.TransactionId }
            }));
        }

        public void SetUserId(string id) {
            Fire(SDK.setCustomerUserId(id));
        }

        // AppsFlyer keeps no user properties.
        public void SetUserProperty(string name, string value) { }

        // RiseOn's mediation ids to AppsFlyer's.
        private static MediationNetwork Mediation(string id) => id switch {
            "admob"     => MediationNetwork.GoogleAdMob
          , "max"       => MediationNetwork.ApplovinMax
          , "levelplay" => MediationNetwork.IronSource
          , _           => MediationNetwork.Custom
        };

        // The calls only hand the request to the native SDK; a failure there is logged, not thrown into the game.
        private static async void Fire(Awaitable call) {
            try {
                await call;
            } catch (Exception exception) {
                Debug.LogException(exception);
            }
        }
    }
}