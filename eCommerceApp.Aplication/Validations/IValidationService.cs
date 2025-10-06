using eCommerceApp.Aplication.DTOs;
using FluentValidation;

namespace eCommerceApp.Aplication.Validations
{
    /// <summary>
    /// Interface chuẩn hóa dịch vụ validate input (sử dụng FluentValidation).
    /// </summary>
    public interface IValidationService
    {
        /// <summary>
        /// Thực hiện validate model bằng validator chỉ định.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu cần validate</typeparam>
        /// <param name="model">Model chứa dữ liệu</param>
        /// <param name="validator">Validator cụ thể cho model đó</param>
        Task<ServiceResponse> ValidateAsync<T>(T model, IValidator<T> validator);
    }
}

