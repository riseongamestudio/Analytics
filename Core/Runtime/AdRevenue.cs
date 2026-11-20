namespace RiseOn.Analytics {
    /// <summary>The revenue of one ad impression, as the mediation reported it.</summary>
    public readonly struct AdRevenue {
        /// <summary>The mediation that served it, by RiseOn's provider id: admob, max, levelplay.</summary>
        public readonly string Mediation;

        /// <summary>The network that won the impression, as the mediation names it.</summary>
        public readonly string Network;

        public readonly string Format;
        public readonly string UnitId;

        /// <summary>In <see cref="Currency"/>, not micros.</summary>
        public readonly double Value;

        /// <summary>ISO 4217.</summary>
        public readonly string Currency;

        /// <summary>Where in the game it showed; null when the game does not say.</summary>
        public readonly string Placement;

        public AdRevenue(string mediation, string network, string format, string unitId, double value, string currency, string placement = null) {
            Mediation = mediation;
            Network   = network;
            Format    = format;
            UnitId    = unitId;
            Value     = value;
            Currency  = currency;
            Placement = placement;
        }

        public override string ToString() {
            return $"{Mediation}/{Network} {Format} {UnitId}: {Value} {Currency}";
        }
    }
}