using eCommerceApp.Aplication.DTOs.Identity;
using FluentValidation;

namespace eCommerceApp.Aplication.Validations.Authentication
{
    /// <summary>
    /// Validator cho LoginUser DTO
    /// Đảm bảo dữ liệu đầu vào khi login hợp lệ.
    /// </summary>
    public class LoginUserValidator : AbstractValidator<LoginUser>
    {
        public LoginUserValidator()
        {
            // Email: bắt buộc và đúng định dạng
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            // Password: bắt buộc
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
}
