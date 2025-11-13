namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IImageStorageService
    {
        Task<string> UploadBase64Async(string base64, string? folder = null, CancellationToken cancellationToken = default);
    }
}