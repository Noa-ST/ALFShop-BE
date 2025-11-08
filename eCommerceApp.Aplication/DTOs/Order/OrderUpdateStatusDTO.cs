namespace eCommerceApp.Aplication.DTOs.Order
{
    public class OrderUpdateStatusDTO
    {
        public string Status { get; set; } = string.Empty; // Pending, Shipping, Completed, Cancelled
    }
}
