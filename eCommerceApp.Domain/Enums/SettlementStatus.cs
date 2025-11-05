namespace eCommerceApp.Domain.Enums
{
    /// <summary>
    /// Trạng thái của Settlement (giải ngân)
    /// </summary>
    public enum SettlementStatus
    {
        /// <summary>
        /// Đang chờ xử lý (Seller đã yêu cầu, chờ Admin approve)
        /// </summary>
        Pending,
        
        /// <summary>
        /// Đã được Admin approve, đang chờ giải ngân
        /// </summary>
        Approved,
        
        /// <summary>
        /// Đang xử lý giải ngân (đã gọi API PayOS/Bank nhưng chưa confirm)
        /// </summary>
        Processing,
        
        /// <summary>
        /// Đã giải ngân thành công
        /// </summary>
        Completed,
        
        /// <summary>
        /// Giải ngân thất bại
        /// </summary>
        Failed,
        
        /// <summary>
        /// Đã bị hủy (bởi Admin hoặc Seller)
        /// </summary>
        Cancelled
    }
}

