namespace eCommerceApp.Domain.Enums
{
    /// <summary>
    /// Phương thức giải ngân cho Seller
    /// </summary>
    public enum SettlementMethod
    {
        /// <summary>
        /// Chuyển khoản ngân hàng
        /// </summary>
        BankTransfer,
        
        /// <summary>
        /// Chuyển qua PayOS (nếu seller có tài khoản PayOS)
        /// </summary>
        PayOS,
        
        /// <summary>
        /// Chuyển qua ví điện tử khác (nếu cần)
        /// </summary>
        Wallet
    }
}

