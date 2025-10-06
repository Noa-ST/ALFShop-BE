namespace eCommerceApp.Aplication.DTOs
{
    /// <summary>
    /// Dùng để chuẩn hóa kết quả trả về từ các Service layer.
    /// - Success: cho biết service xử lý thành công hay thất bại.
    /// - Message: mô tả chi tiết kết quả hoặc lỗi (nếu có).
    /// </summary>
    public record ServiceResponse(bool Success = false, string? Message = null);
}
