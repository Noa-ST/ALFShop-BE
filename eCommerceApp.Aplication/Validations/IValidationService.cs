using eCommerceApp.Aplication.DTOs;
using FluentValidation;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace eCommerceApp.Aplication.Validations
{
    public interface IValidationService
    {
        Task<ServiceResponse> ValidateAsync<T>(T model, IValidator<T> validator);
    }
}

