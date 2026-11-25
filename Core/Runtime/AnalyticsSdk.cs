using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using RiseOn.Consent;
using UnityEngine;

namespace RiseOn.Analytics {
    /// <summary>
    /// The game's one way to analytics.<br/>
    /// Every call goes to each installed service: a service starts on its own once what it needs is ready, and gets what came before then.<br/>
    /// An event the project settings do not send to a service skips it; an event a service would refuse stops the Editor and Development Builds, and is dropped in a release.<br/>
    /// Unity's thread only.
    /// </summary>
    public static class AnalyticsSdk {
        // What a service holds before it starts; beyond it the oldest calls go, since a service that never starts
        // must not grow the game's memory.
        private const int QueueLimit = 1000;

        private static readonly List<Sink> sinks = new();

        /// <summary>The Editor's own entry among the services: it prints what the game logs to the Console, and the page filters its events as a service's.</summary>
        internal const string EditorSinkId = "editor";

        private static AnalyticsSettings settings;

        internal static AnalyticsSettings Settings => settings ??= AnalyticsSettings.Load();

        /// <summary>Called by a service's package before the first scene loads; one service per id.</summary>
        public static void Register(IAnalyticsSink sink) {
            if (sinks.Any(other => other.Target.Id == sink.Id)) throw new InvalidOperationException($"{nameof(AnalyticsSdk)}: a service named {sink.Id} is already registered");

            // The Editor has none of the services' native SDKs: a service there only checks the events it would get.
            var entry = new Sink(sink);
            sinks.Add(entry);
            if (Application.isEditor) {
                entry.Stop();
            } else {
                Start(entry);
            }
        }

        /// <param name="event">One of the events of Project Settings > RiseOn > Analytics; a number cast to it that is none of them is dropped with an error.</param>
        public static void Log(AnalyticsEvent @event, params AnalyticsParameter[] parameters) {
            var name = AnalyticsEventNames.Of(@event);
            if (name == null) {
                Debug.LogError($"{nameof(AnalyticsSdk)}: {(int)@event} is none of the events of Project Settings > RiseOn > Analytics, so it was not sent");
                return;
            }

            parameters ??= Array.Empty<AnalyticsParameter>();
            if (Application.isEditor && Settings.LogInEditor && Settings.Sends(EditorSinkId, @event)) Debug.Log($"{nameof(AnalyticsSdk)}: {name} {string.Join(", ", parameters)}");

            var problems = new List<string>();
            foreach (var sink in sinks) {
                if (!Settings.Sends(sink.Target.Id, @event)) continue;

                var found = sink.Target.Check(name, parameters).ToList();
                if (found.Count > 0) {
                    problems.AddRange(found.Select(problem => $"{sink.Target.Id}: {problem}"));
                    continue;
                }

                sink.Call(target => target.Log(name, parameters));
            }

            if (problems.Count == 0) return;

            var message = $"{nameof(AnalyticsSdk)}: event {name} was not sent:\n- " + string.Join("\n- ", problems);
            if (Debug.isDebugBuild) throw new ArgumentException(message);
            Debug.LogError(message);
        }

        /// <summary>As <see cref="Log(AnalyticsEvent, AnalyticsParameter[])"/> with one parameter.</summary>
        public static void Log(AnalyticsEvent @event, string parameterName, string parameterValue) => Log(@event, new AnalyticsParameter(parameterName, parameterValue));

        /// <summary>As <see cref="Log(AnalyticsEvent, AnalyticsParameter[])"/> with one parameter.</summary>
        public static void Log(AnalyticsEvent @event, string parameterName, long parameterValue) => Log(@event, new AnalyticsParameter(parameterName, parameterValue));

        /// <summary>As <see cref="Log(AnalyticsEvent, AnalyticsParameter[])"/> with one parameter.</summary>
        public static void Log(AnalyticsEvent @event, string parameterName, double parameterValue) => Log(@event, new AnalyticsParameter(parameterName, parameterValue));

        /// <summary>
        /// An ad's revenue, to each service the Revenue table of Project Settings > RiseOn > Analytics ticks, through its standard call: Google Analytics' ad_impression, AppsFlyer's logAdRevenue.<br/>
        /// com.riseon.ads calls it for every ad it shows, unless the page's Ad Revenue from Ads is off.
        /// </summary>
        public static void LogAdRevenue(in AdRevenue revenue) {
            if (Application.isEditor && Settings.LogInEditor && Settings.Takes(EditorSinkId, AnalyticsRevenue.Ad)) Debug.Log($"{nameof(AnalyticsSdk)}: ad revenue {revenue}");

            var copy = revenue;
            foreach (var sink in sinks) {
                if (Settings.Takes(sink.Target.Id, AnalyticsRevenue.Ad)) sink.Call(target => target.LogAdRevenue(copy));
            }
        }

        /// <summary>
        /// A store purchase, once, after the store confirmed it, to each service the Revenue table of Project Settings > RiseOn > Analytics ticks, through its standard call: Google Analytics' in_app_purchase, AppsFlyer's af_purchase.<br/>
        /// com.riseon.iap calls it for every purchase it confirms, unless the page's Purchases from IAP is off.
        /// </summary>
        public static void LogPurchase(in PurchaseRevenue purchase) {
            if (Application.isEditor && Settings.LogInEditor && Settings.Takes(EditorSinkId, AnalyticsRevenue.Purchase)) Debug.Log($"{nameof(AnalyticsSdk)}: purchase {purchase}");

            var copy = purchase;
            foreach (var sink in sinks) {
                if (Settings.Takes(sink.Target.Id, AnalyticsRevenue.Purchase)) sink.Call(target => target.LogPurchase(copy));
            }
        }

        public static void SetUserId(string id) {
            foreach (var sink in sinks) sink.Call(target => target.SetUserId(id));
        }

        public static void SetUserProperty(string name, string value) {
            foreach (var sink in sinks) sink.Call(target => target.SetUserProperty(name, value));
        }

        // Static state outlives a play session when the Editor skips the domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() {
            sinks.Clear();
            settings = null;
        }

        // After every SubsystemRegistration, the consent flow's reset of its own event included.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ListenToConsent() {
            ConsentFlow.StateChanged += ApplyConsent;
        }

        private static void ApplyConsent(ConsentState consent) {
            foreach (var sink in sinks) sink.Call(target => target.ApplyConsent(consent));
        }

        private static void Start(Sink sink) {
            StartAsync().Forget();

            async UniTaskVoid StartAsync() {
                bool started;
                try {
                    started = await sink.Target.StartAsync();
                } catch (Exception exception) {
                    Debug.LogException(exception);
                    started = false;
                }

                if (started) {
                    sink.Run();
                } else {
                    Debug.LogWarning($"{nameof(AnalyticsSdk)}: {sink.Target.Id} did not start, its calls this session are dropped");
                    sink.Stop();
                }
            }
        }

        private sealed class Sink {
            public readonly IAnalyticsSink Target;

            private readonly Queue<Action<IAnalyticsSink>> pending = new();
            private bool running;
            private bool stopped;
            private bool overflowed;

            public Sink(IAnalyticsSink target) {
                Target = target;
            }

            public void Call(Action<IAnalyticsSink> call) {
                if (stopped) return;

                if (!running) {
                    if (pending.Count is QueueLimit) {
                        pending.Dequeue();
                        if (!overflowed) Debug.LogWarning($"{nameof(AnalyticsSdk)}: {Target.Id} has not started after {QueueLimit} calls; the oldest are dropped");
                        overflowed = true;
                    }

                    pending.Enqueue(call);
                    return;
                }

                Invoke(call);
            }

            public void Run() {
                running = true;
                while (pending.Count > 0) Invoke(pending.Dequeue());
            }

            public void Stop() {
                stopped = true;
                pending.Clear();
            }

            // One service failing a call must not stop the game, nor the other services.
            private void Invoke(Action<IAnalyticsSink> call) {
                try {
                    call(Target);
                } catch (Exception exception) {
                    Debug.LogException(exception);
                }
            }
        }
    }
}