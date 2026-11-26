using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiseOn.Analytics {
    /// <summary>One event of Project Settings > RiseOn > Analytics: the name every service gets, and its member of <see cref="AnalyticsEvent"/>.</summary>
    [Serializable]
    internal sealed class AnalyticsEventEntry {
        [SerializeField]
        [Tooltip("The number of the event's enum member: random, so events two branches add do not meet on one number, and kept, so enum fields keep their value when events are renamed or removed.")]
        private int id;

        [SerializeField]
        [Tooltip("The name the services get, and the event's member of AnalyticsEvent in code.")]
        private string name = "";

        [SerializeField]
        private string description = "";

        // The services the Events table unticked for it, by id; every other service takes it, one installed later included.
        [SerializeField]
        private List<string> off = new();

        public int Id { get => id; internal set => id = value; }
        public string Name { get => name; set => name = value; }
        public string Description { get => description; set => description = value; }

        /// <summary>Whether the service takes the event, as its tick in the Events table says.</summary>
        public bool Sends(string sink) => off == null || !off.Contains(sink);

        /// <summary>Ticks or unticks the service in the event's row.</summary>
        internal void SetSends(string sink, bool on) {
            off ??= new List<string>();
            off.Remove(sink);
            if (!on) off.Add(sink);
        }
    }
}