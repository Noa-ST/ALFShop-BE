using System.Net;

namespace eCommerceApp.Aplication.DTOs
{
    /// <summary>
    /// Dùng để chuẩn hóa kết quả trả về từ các Service layer (Non-Generic).
    /// </summary>
    public record ServiceResponse(
        bool Succeeded = false,
        string? Message = null,
        // Thêm Status Code để hỗ trợ Controller trả về mã HTTP chính xác
        HttpStatusCode StatusCode = HttpStatusCode.InternalServerError
    )
    {
        public static ServiceResponse Success(string message = "Thành công.") =>
            new ServiceResponse(true, message, HttpStatusCode.OK);

        public static ServiceResponse Fail(string message = "Thất bại.", HttpStatusCode statusCode = HttpStatusCode.BadRequest) =>
            new ServiceResponse(false, message, statusCode);
    }

    /// <summary>
    /// Dùng để chuẩn hóa kết quả trả về kèm theo Data (Generic).
    /// </summary>
    public record ServiceResponse<T>(
        bool Succeeded = false,
        T? Data = default,
        string? Message = null,
        // Kế thừa Status Code
        HttpStatusCode StatusCode = HttpStatusCode.InternalServerError
    ) : ServiceResponse(Succeeded, Message, StatusCode)
    {
        public static ServiceResponse<T> Success(T data, string message = "Thành công.") =>
            new ServiceResponse<T>(true, data, message, HttpStatusCode.OK);

        public new static ServiceResponse<T> Fail(string message = "Thất bại.", HttpStatusCode statusCode = HttpStatusCode.BadRequest) =>
            new ServiceResponse<T>(false, default, message, statusCode);

        public static ServiceResponse<T> Fail(string message, T data, HttpStatusCode statusCode = HttpStatusCode.BadRequest) =>
            new ServiceResponse<T>(false, data, message, statusCode);
    }
}