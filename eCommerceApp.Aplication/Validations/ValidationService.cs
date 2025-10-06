using eCommerceApp.Aplication.DTOs;
using FluentValidation;

namespace eCommerceApp.Aplication.Validations
{
    /// <summary>
    /// Cài đặt cho IValidationService
    /// Dùng FluentValidation để validate DTOs.
    /// Gom các lỗi về dạng chuỗi Message duy nhất.
    /// </summary>
    public class ValidationService : IValidationService
    {
        public async Task<ServiceResponse> ValidateAsync<T>(T model, IValidator<T> validator)
        {
            var validationResult = await validator.ValidateAsync(model);

            if (!validationResult.IsValid)
            {
                // Gộp tất cả error message lại thành một chuỗi
                var errors = validationResult.Errors
                                             .Select(e => e.ErrorMessage)
                                             .ToList();
                string errorsToString = string.Join("; ", errors);

                return new ServiceResponse(false, errorsToString);
            }

            return new ServiceResponse(true, "Validation successful");
        }
    }
}

