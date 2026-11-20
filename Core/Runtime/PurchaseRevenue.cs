namespace RiseOn.Analytics {
    /// <summary>One completed store purchase, logged once, after the store confirmed it.</summary>
    public readonly struct PurchaseRevenue {
        public readonly string ProductId;

        /// <summary>The price the player paid, in <see cref="Currency"/>.</summary>
        public readonly decimal Value;

        /// <summary>ISO 4217, from the store's localized price.</summary>
        public readonly string Currency;

        public readonly int Quantity;

        /// <summary>The store's id of this transaction: Google Play's order id, the App Store's transaction id.</summary>
        public readonly string TransactionId;

        public PurchaseRevenue(string productId, decimal value, string currency, string transactionId, int quantity = 1) {
            ProductId     = productId;
            Value         = value;
            Currency      = currency;
            TransactionId = transactionId;
            Quantity      = quantity;
        }

        public override string ToString() => $"{ProductId} x{Quantity}: {Value} {Currency} ({TransactionId})";
    }
}