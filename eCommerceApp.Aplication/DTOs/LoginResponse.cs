namespace eCommerceApp.Aplication.DTOs
{
    public record LoginResponse
    (bool Success = false,
    string Message = null!,
    string Token = null!,
    string RefreshToken = null!,

    string Role = null!,
    string UserId = "",
    string Fullname = null!);
}