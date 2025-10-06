using eCommerceApp.Aplication.DTOs.Identity;
using FluentValidation;

namespace eCommerceApp.Aplication.Validations.Authentication
{
    /// <summary>
    /// Validator cho CreateUser DTO
    /// Đảm bảo dữ liệu đầu vào khi đăng ký user hợp lệ.
    /// </summary>
    public class CreateUserValidator : AbstractValidator<CreateUser>
    {
        public CreateUserValidator()
        {
            // Fullname không được bỏ trống
            RuleFor(x => x.Fullname)
                .NotEmpty().WithMessage("Full name is required.");

            // Email phải có và đúng định dạng
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            // Password: kiểm tra nhiều điều kiện
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"\d").WithMessage("Password must contain at least one number.")
                .Matches(@"[^\w]").WithMessage("Password must contain at least one special character.");

            // ConfirmPassword phải khớp với Password
            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Passwords do not match.");
        }
    }
}
