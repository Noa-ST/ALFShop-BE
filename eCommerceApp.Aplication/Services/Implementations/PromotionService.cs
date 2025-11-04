using AutoMapper;
using eCommerceApp.Aplication.DTOs.Promotion;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces; // <-- THÊM DÒNG NÀY ĐỂ SỬA LỖI
using FluentValidation;
using System.Threading.Tasks;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class PromotionService : IPromotionService
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreatePromotionDto> _validator;
        private readonly IUnitOfWork _unitOfWork; // Lỗi của bạn là ở đây

        public PromotionService(
            IPromotionRepository promotionRepository,
            IMapper mapper,
            IValidator<CreatePromotionDto> validator,
            IUnitOfWork unitOfWork) // Và ở đây
        {
            _promotionRepository = promotionRepository;
            _mapper = mapper;
            _validator = validator;
            _unitOfWork = unitOfWork;
        }

        public async Task<PromotionDto> CreatePromotionAsync(CreatePromotionDto createDto)
        {
            // 1. Validate
            var validationResult = await _validator.ValidateAsync(createDto);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            // 2. Map
            var promotionEntity = _mapper.Map<Promotion>(createDto);

            // 3. Add
            await _promotionRepository.AddAsync(promotionEntity);

            // 4. Save
            await _unitOfWork.SaveChangesAsync();

            // 5. Return DTO
            var promotionDto = _mapper.Map<PromotionDto>(promotionEntity);

            return promotionDto;
        }
    }
}