namespace eCommerceApp.Aplication.DTOs.Payment
{
    /// <summary>
    /// Request để tạo payment link từ PayOS
    /// </summary>
    public class PayOSCreatePaymentRequest
    {
        public int OrderCode { get; set; } // Mã đơn hàng (int từ PayOS)
        public int Amount { get; set; } // Số tiền (VND)
        public string Description { get; set; } = string.Empty; // Mô tả đơn hàng
        public List<PayOSItem> Items { get; set; } = new(); // Danh sách sản phẩm
        public string CancelUrl { get; set; } = string.Empty; // URL khi hủy
        public string ReturnUrl { get; set; } = string.Empty; // URL khi thành công
        public long? ExpiredAt { get; set; } // Thời gian hết hạn (Unix timestamp)
    }

    public class PayOSItem
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int Price { get; set; }
    }
}

