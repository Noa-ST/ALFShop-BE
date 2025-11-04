using eCommerceApp.Aplication.DTOs.Promotion;
using eCommerceApp.Domain.Interfaces; 
using FluentValidation;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace eCommerceApp.Aplication.Validations
{
    public class CreatePromotionDtoValidator : AbstractValidator<CreatePromotionDto>
    {
        private readonly IPromotionRepository _promotionRepository;

        public CreatePromotionDtoValidator(IPromotionRepository promotionRepository)
        {
            _promotionRepository = promotionRepository;

            // --- QUY TẮC CƠ BẢN ---
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã khuyến mãi là bắt buộc.")
                .MaximumLength(50).WithMessage("Mã khuyến mãi không quá 50 ký tự.")
                .MustAsync(BeUniqueCode).WithMessage("Mã khuyến mãi này đã tồn tại.");

            RuleFor(x => x.DiscountValue)
                .GreaterThan(0).WithMessage("Giá trị giảm phải lớn hơn 0.");

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate).WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");

            RuleFor(x => x.MaxUsageCount)
                .GreaterThan(0).WithMessage("Số lần sử dụng tối đa phải lớn hơn 0.");

            // --- QUY TẮC NGHIỆP VỤ NÂNG CAO ---

            // 1. Chỉ được giảm giá cho Product HOẶC Shop, không được cả hai.
            RuleFor(x => x)
                .Must(x => !x.ProductId.HasValue || !x.ShopId.HasValue)
                .WithMessage("Chỉ có thể áp dụng khuyến mãi cho Cửa hàng hoặc Sản phẩm, không phải cả hai.");

            // 2. Nếu là giảm theo %, giá trị không được quá 100
            RuleFor(x => x.DiscountValue)
                .LessThanOrEqualTo(100)
                .When(x => x.PromotionType == Domain.Enums.PromotionType.Percentage)
                .WithMessage("Giá trị giảm % không được quá 100.");
        }

        // Hàm helper để kiểm tra Code có bị trùng không
        private async Task<bool> BeUniqueCode(string code, CancellationToken cancellationToken)
        {
            var existingPromotion = await _promotionRepository.GetByCodeAsync(code);
            return existingPromotion == null;
        }
    }
}