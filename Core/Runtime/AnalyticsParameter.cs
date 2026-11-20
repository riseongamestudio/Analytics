using System.Globalization;

namespace RiseOn.Analytics {
    public enum AnalyticsValueKind {
        Text
      , Integer
      , Number
    }

    /// <summary>One event parameter: a name and a text, whole or decimal value, the kinds every service takes.</summary>
    public readonly struct AnalyticsParameter {
        public readonly string             Name;
        public readonly AnalyticsValueKind Kind;
        public readonly string             Text;
        public readonly long               Integer;
        public readonly double             Number;

        public AnalyticsParameter(string name, string value) {
            Name    = name;
            Kind    = AnalyticsValueKind.Text;
            Text    = value ?? "";
            Integer = 0;
            Number  = 0;
        }

        public AnalyticsParameter(string name, long value) {
            Name    = name;
            Kind    = AnalyticsValueKind.Integer;
            Text    = null;
            Integer = value;
            Number  = 0;
        }

        public AnalyticsParameter(string name, double value) {
            Name    = name;
            Kind    = AnalyticsValueKind.Number;
            Text    = null;
            Integer = 0;
            Number  = value;
        }

        /// <summary>The value as text, the same on every device: a service that takes strings only reads this.</summary>
        public string ValueText => Kind switch {
            AnalyticsValueKind.Integer => Integer.ToString(CultureInfo.InvariantCulture)
          , AnalyticsValueKind.Number  => Number.ToString("R", CultureInfo.InvariantCulture)
          , _                          => Text
        };

        public override string ToString() => $"{Name}={ValueText}";
    }
}