using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Firebase.Analytics;
using RiseOn.Consent;
using RiseOn.Firebase;
using UnityEngine;

namespace RiseOn.Analytics.Firebase {
    /// <summary>
    /// Google Analytics, Firebase's analytics.<br/>
    /// It starts with com.riseon.firebase's app.<br/>
    /// Consent needs nothing here: Google's UMP updates Analytics' consent mode itself where EU rules apply, once consent mode is on in the AdMob console.
    /// </summary>
    internal sealed class FirebaseAnalyticsSink : IAnalyticsSink {
        public const string SinkId = "firebase";

        // FirebaseAnalytics.logEvent's rules: firebase.google.com/docs/reference/android/com/google/firebase/analytics/FirebaseAnalytics.Event
        private static readonly Regex namePattern = new("^[A-Za-z][A-Za-z0-9_]{0,39}$");

        private static readonly Regex userPropertyNamePattern = new("^[A-Za-z][A-Za-z0-9_]{0,23}$");

        private static readonly string[] reservedPrefixes = { "firebase_", "google_", "ga_" };

        private static readonly HashSet<string> reservedEvents = new() {
            "ad_activeview", "ad_click", "ad_exposure", "ad_query", "ad_reward", "adunit_exposure", "app_background", "app_clear_data"
          , "app_exception", "app_remove", "app_store_refund", "app_store_subscription_cancel", "app_store_subscription_convert"
          , "app_store_subscription_renew", "app_update", "app_upgrade", "dynamic_link_app_open", "dynamic_link_app_update"
          , "dynamic_link_first_open", "error", "first_open", "first_visit", "notification_dismiss", "notification_foreground"
          , "notification_open", "notification_receive", "os_update", "session_start", "session_start_with_rollout", "user_engagement"
        };

        private const int MaxParameters = 25;
        private const int MaxTextLength = 100;

        public string Id => SinkId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() {
            AnalyticsSdk.Register(new FirebaseAnalyticsSink());
        }

        /// <summary>Why Firebase refuses an event of this name; the settings page checks each event with it too.</summary>
        internal static IEnumerable<string> EventNameProblems(string name) {
            if (!namePattern.IsMatch(name ?? "")) yield return $"\"{name}\" is not an event name: 1 to 40 letters, digits or _, starting with a letter";
            if (reservedEvents.Contains(name)) yield return $"{name} is reserved by Firebase";
            if (reservedPrefixes.Any(prefix => name?.StartsWith(prefix) == true)) yield return $"{name} starts with a prefix Firebase reserves";
        }

        public IEnumerable<string> Check(string name, IReadOnlyList<AnalyticsParameter> parameters) {
            foreach (var problem in EventNameProblems(name)) yield return problem;
            if (parameters.Count > MaxParameters) yield return $"{parameters.Count} parameters, Firebase keeps {MaxParameters}";

            foreach (var parameter in parameters) {
                if (!namePattern.IsMatch(parameter.Name ?? "")) yield return $"\"{parameter.Name}\" is not a parameter name: 1 to 40 letters, digits or _, starting with a letter";
                if (reservedPrefixes.Any(prefix => parameter.Name?.StartsWith(prefix) == true)) yield return $"{parameter.Name} starts with a prefix Firebase reserves";
                if (parameter.Kind is AnalyticsValueKind.Text && parameter.Text.Length > MaxTextLength) yield return $"{parameter.Name} is {parameter.Text.Length} characters, Firebase cuts it to {MaxTextLength}";
            }

            foreach (var duplicate in parameters.GroupBy(parameter => parameter.Name).Where(group => group.Count() > 1).Select(group => group.Key)) {
                yield return $"{duplicate} is given twice";
            }
        }

        // Analytics has no start of its own: it runs with the Firebase app com.riseon.firebase starts.
        public async UniTask<bool> StartAsync() {
            await FirebaseInit.WaitUntilCompleteAsync();
            return true;
        }

        public void ApplyConsent(ConsentState consent) { }

        public void Log(string name, IReadOnlyList<AnalyticsParameter> parameters) {
            FirebaseAnalytics.LogEvent(name, parameters.Select(ToParameter).ToArray());
        }

        public void LogAdRevenue(in AdRevenue revenue) {
            var parameters = new List<Parameter> {
                new(FirebaseAnalytics.ParameterAdPlatform, revenue.Mediation ?? "")
              , new(FirebaseAnalytics.ParameterAdSource, revenue.Network ?? "")
              , new(FirebaseAnalytics.ParameterAdFormat, revenue.Format ?? "")
              , new(FirebaseAnalytics.ParameterAdUnitName, revenue.UnitId ?? "")
              , new(FirebaseAnalytics.ParameterValue, revenue.Value)
              , new(FirebaseAnalytics.ParameterCurrency, revenue.Currency ?? "")
            };

            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventAdImpression, parameters.ToArray());
        }

        // Google Play purchases reach Analytics on their own once the Firebase project is linked to Play, else as the in_app_purchase
        // event the app logs; App Store ones made with StoreKit 2, which Unity IAP 5 uses, only through LogAppleTransactionAsync:
        // firebase.google.com/docs/analytics/unity/measure-in-app-purchases
        public void LogPurchase(in PurchaseRevenue purchase) {
            if (Application.platform is RuntimePlatform.Android) {
                if (FirebaseAnalyticsSettings.Load().GooglePlayLinked) return;

                FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventInAppPurchase, new Parameter(FirebaseAnalytics.ParameterProductName, purchase.ProductId ?? ""),
                                           new Parameter(FirebaseAnalytics.ParameterValue, (double) purchase.Value), new Parameter(FirebaseAnalytics.ParameterCurrency, purchase.Currency ?? ""),
                                           new Parameter(FirebaseAnalytics.ParameterQuantity, purchase.Quantity));
                return;
            }

            if (Application.platform is not RuntimePlatform.IPhonePlayer) return;

            if (string.IsNullOrEmpty(purchase.TransactionId)) {
                Debug.LogError($"{nameof(FirebaseAnalyticsSink)}: {purchase.ProductId} has no transaction id, Analytics cannot log it");
                return;
            }

            var productId = purchase.ProductId;
            FirebaseAnalytics.LogAppleTransactionAsync(purchase.TransactionId).ContinueWith(task => {
                if (task.IsFaulted || task.IsCanceled) Debug.LogError($"{nameof(FirebaseAnalyticsSink)}: {productId} was not logged: {task.Exception?.GetBaseException().Message ?? "canceled"}");
            });
        }

        public void SetUserId(string id) {
            FirebaseAnalytics.SetUserId(id);
        }

        // setUserProperty's rules: a name of 1 to 24 letters, digits or _, a value of up to 36 characters.
        public void SetUserProperty(string name, string value) {
            if (!userPropertyNamePattern.IsMatch(name ?? "") || reservedPrefixes.Any(prefix => name?.StartsWith(prefix) == true)) {
                Debug.LogError($"{nameof(FirebaseAnalyticsSink)}: \"{name}\" is not a user property name Firebase takes");
                return;
            }

            if (value?.Length > 36) Debug.LogError($"{nameof(FirebaseAnalyticsSink)}: user property {name} is {value.Length} characters, Firebase drops more than 36");
            FirebaseAnalytics.SetUserProperty(name, value);
        }

        private static Parameter ToParameter(AnalyticsParameter parameter) {
            return parameter.Kind switch {
                AnalyticsValueKind.Integer => new Parameter(parameter.Name, parameter.Integer)
              , AnalyticsValueKind.Number  => new Parameter(parameter.Name, parameter.Number)
              , AnalyticsValueKind.Text    => new Parameter(parameter.Name, parameter.Text)
              , _                          => throw new ArgumentOutOfRangeException(nameof(parameter))
            };
        }
    }
}