namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task<bool> SendPasswordResetEmailAsync(string to, string resetToken, string userName);
        Task<bool> SendEmailConfirmationEmailAsync(string to, string confirmationToken, string userName);
    }
}

