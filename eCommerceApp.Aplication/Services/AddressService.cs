// File: eCommerceApp.Aplication/Services/Implementations/AddressService.cs

using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Address;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Linq;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class AddressService : IAddressService
    {
        private readonly IAddressRepository _addressRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public AddressService(IAddressRepository addressRepository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _addressRepository = addressRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        // ✅ POST /api/Address/create
        public async Task<ServiceResponse> CreateAddressAsync(string userId, CreateAddress dto)
        {
            // ✅ Validate userId
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResponse.Fail("Không thể xác định người dùng.", HttpStatusCode.Unauthorized);
            }

            // ✅ New: Validate phone number format
            if (!IsValidVietnamesePhoneNumber(dto.PhoneNumber))
            {
                return ServiceResponse.Fail("Số điện thoại không hợp lệ. Vui lòng nhập số điện thoại Việt Nam (10-11 số, bắt đầu bằng 0 hoặc +84).", HttpStatusCode.BadRequest);
            }

            // ✅ New: Check duplicate address
            bool isDuplicate = await _addressRepository.IsDuplicateAddressAsync(
                userId, 
                dto.FullStreet, 
                dto.Ward, 
                dto.District, 
                dto.Province);

            if (isDuplicate)
            {
                return ServiceResponse.Fail("Địa chỉ này đã tồn tại trong danh sách của bạn.", HttpStatusCode.BadRequest);
            }

            // ✅ New: Limit số lượng địa chỉ (max 10)
            const int MAX_ADDRESSES_PER_USER = 10;
            int currentCount = await _addressRepository.CountUserAddressesAsync(userId);
            if (currentCount >= MAX_ADDRESSES_PER_USER)
            {
                return ServiceResponse.Fail($"Bạn đã đạt giới hạn tối đa {MAX_ADDRESSES_PER_USER} địa chỉ. Vui lòng xóa địa chỉ cũ trước khi thêm mới.", HttpStatusCode.BadRequest);
            }

            // 1. Kiểm tra số lượng địa chỉ hiện có
            var existingAddresses = await _addressRepository.GetUserAddressesAsync(userId);
            var existingCount = existingAddresses.Count();

            // 2. Ánh xạ và gán UserId
            var address = _mapper.Map<Address>(dto);
            address.UserId = userId;
            address.CreatedAt = DateTime.UtcNow;

            // 3. ✅ Fix: Xử lý logic IsDefault đúng cách
            if (existingCount == 0)
            {
                // Nếu là địa chỉ đầu tiên, tự động đặt làm mặc định
                address.IsDefault = true;
            }
            else if (dto.IsDefault)
            {
                // Nếu địa chỉ mới là mặc định, hủy đặt mặc định cho các địa chỉ cũ
                await _addressRepository.UnsetAllDefaultsAsync(userId);
                address.IsDefault = true;
            }
            else
            {
                address.IsDefault = false;
            }

            // 4. Lưu address mới
            await _addressRepository.AddAsync(address);

            // 5. ✅ Commit vào database
            await _unitOfWork.SaveChangesAsync();

            // 6. ✅ Nếu dto.IsDefault = true, đảm bảo set làm default (đã set ở trên)
            // Không cần gọi SetDefaultAddressAsync vì address chưa có Id trong DB

            return ServiceResponse.Success("Thêm địa chỉ giao hàng thành công.");
        }

        // ✅ PUT /api/Address/update/{id}
        public async Task<ServiceResponse> UpdateAddressAsync(string userId, Guid addressId, UpdateAddress dto)
        {
            // ✅ Validate userId
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResponse.Fail("Không thể xác định người dùng.", HttpStatusCode.Unauthorized);
            }

            // ✅ Validate addressId với dto.Id
            if (addressId != dto.Id)
            {
                return ServiceResponse.Fail("ID trong route không khớp với ID trong body.", HttpStatusCode.BadRequest);
            }

            // ✅ New: Validate phone number format
            if (!IsValidVietnamesePhoneNumber(dto.PhoneNumber))
            {
                return ServiceResponse.Fail("Số điện thoại không hợp lệ. Vui lòng nhập số điện thoại Việt Nam (10-11 số, bắt đầu bằng 0 hoặc +84).", HttpStatusCode.BadRequest);
            }

            var existingAddress = await _addressRepository.GetByIdAsync(addressId);

            // 1. Kiểm tra quyền sở hữu
            if (existingAddress == null || existingAddress.UserId != userId || existingAddress.IsDeleted)
            {
                return ServiceResponse.Fail("Không tìm thấy địa chỉ hoặc không có quyền truy cập.", HttpStatusCode.NotFound);
            }

            // ✅ New: Check duplicate address (exclude current address)
            bool isDuplicate = await _addressRepository.IsDuplicateAddressAsync(
                userId, 
                dto.FullStreet, 
                dto.Ward, 
                dto.District, 
                dto.Province,
                addressId); // Exclude current address

            if (isDuplicate)
            {
                return ServiceResponse.Fail("Địa chỉ này đã tồn tại trong danh sách của bạn.", HttpStatusCode.BadRequest);
            }

            // 2. ✅ Fix: Xử lý logic IsDefault đúng cách
            bool shouldSetDefault = dto.IsDefault && !existingAddress.IsDefault;
            bool shouldUnsetDefault = !dto.IsDefault && existingAddress.IsDefault;

            // 3. Ánh xạ và cập nhật các field từ DTO (trước khi xử lý IsDefault)
            _mapper.Map(dto, existingAddress);
            existingAddress.UpdatedAt = DateTime.UtcNow;

            // 4. Xử lý logic IsDefault và unset/set default cho các address khác
            if (shouldSetDefault)
            {
                // Nếu muốn set làm default, hủy default của các address khác
                await _addressRepository.UnsetAllDefaultsAsync(userId);
                existingAddress.IsDefault = true;
            }
            else if (shouldUnsetDefault)
            {
                // Nếu muốn hủy default của address này
                existingAddress.IsDefault = false;
                // Tự động set address khác làm default (nếu có)
                await _addressRepository.SetFirstAddressAsDefaultAsync(userId);
            }
            // Nếu IsDefault không thay đổi, giữ nguyên giá trị hiện tại

            // 5. Update address và commit vào database
            await _addressRepository.UpdateAsync(existingAddress);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResponse.Success("Cập nhật địa chỉ thành công.");
        }

        // ✅ DELETE /api/Address/delete/{id}
        public async Task<ServiceResponse> DeleteAddressAsync(string userId, Guid addressId)
        {
            // ✅ Validate userId
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResponse.Fail("Không thể xác định người dùng.", HttpStatusCode.Unauthorized);
            }

            var existingAddress = await _addressRepository.GetByIdAsync(addressId);

            // 1. Kiểm tra quyền sở hữu
            if (existingAddress == null || existingAddress.UserId != userId || existingAddress.IsDeleted)
            {
                return ServiceResponse.Fail("Không tìm thấy địa chỉ hoặc không có quyền truy cập.", HttpStatusCode.NotFound);
            }

            // 2. Lưu thông tin IsDefault trước khi xóa
            bool wasDefault = existingAddress.IsDefault;

            // 3. Soft Delete
            existingAddress.IsDeleted = true;
            existingAddress.UpdatedAt = DateTime.UtcNow;
            await _addressRepository.UpdateAsync(existingAddress);
            
            // ✅ Commit soft delete vào database
            await _unitOfWork.SaveChangesAsync();

            // 4. ✅ Fix: Nếu xóa địa chỉ mặc định, tự động đặt địa chỉ khác làm mặc định (nếu có)
            if (wasDefault)
            {
                var setSuccess = await _addressRepository.SetFirstAddressAsDefaultAsync(userId);
                // SetFirstAddressAsDefaultAsync() đã có SaveChangesAsync() bên trong
            }

            return ServiceResponse.Success("Xóa địa chỉ thành công.");
        }

        // ✅ GET /api/Address/list
        public async Task<ServiceResponse<IEnumerable<GetAddressDto>>> GetUserAddressesAsync(string userId)
        {
            // ✅ Validate userId
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResponse<IEnumerable<GetAddressDto>>.Fail("Không thể xác định người dùng.", HttpStatusCode.Unauthorized);
            }

            var addresses = await _addressRepository.GetUserAddressesAsync(userId);
            var dtos = _mapper.Map<IEnumerable<GetAddressDto>>(addresses);

            // ✅ Đảm bảo trả về 200 OK với empty array nếu không có address
            return ServiceResponse<IEnumerable<GetAddressDto>>.Success(dtos);
        }

        // ✅ New: GET /api/Address/{id}
        public async Task<ServiceResponse<GetAddressDto>> GetAddressByIdAsync(string userId, Guid addressId)
        {
            // ✅ Validate userId
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResponse<GetAddressDto>.Fail("Không thể xác định người dùng.", HttpStatusCode.Unauthorized);
            }

            var address = await _addressRepository.GetByIdAsync(addressId);

            // Kiểm tra quyền sở hữu
            if (address == null || address.UserId != userId || address.IsDeleted)
            {
                return ServiceResponse<GetAddressDto>.Fail("Không tìm thấy địa chỉ hoặc không có quyền truy cập.", HttpStatusCode.NotFound);
            }

            var dto = _mapper.Map<GetAddressDto>(address);
            return ServiceResponse<GetAddressDto>.Success(dto);
        }

        // ✅ New: PUT /api/Address/{id}/set-default
        public async Task<ServiceResponse> SetDefaultAddressAsync(string userId, Guid addressId)
        {
            // ✅ Validate userId
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResponse.Fail("Không thể xác định người dùng.", HttpStatusCode.Unauthorized);
            }

            var address = await _addressRepository.GetByIdAsync(addressId);

            // Kiểm tra quyền sở hữu
            if (address == null || address.UserId != userId || address.IsDeleted)
            {
                return ServiceResponse.Fail("Không tìm thấy địa chỉ hoặc không có quyền truy cập.", HttpStatusCode.NotFound);
            }

            // Nếu đã là default, không cần làm gì
            if (address.IsDefault)
            {
                return ServiceResponse.Success("Địa chỉ này đã là địa chỉ mặc định.");
            }

            // Set làm default
            await _addressRepository.SetDefaultAddressAsync(userId, addressId);
            return ServiceResponse.Success("Đã đặt làm địa chỉ mặc định thành công.");
        }

        // ✅ New: GET /api/Address/default
        public async Task<ServiceResponse<GetAddressDto>> GetDefaultAddressAsync(string userId)
        {
            // ✅ Validate userId
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResponse<GetAddressDto>.Fail("Không thể xác định người dùng.", HttpStatusCode.Unauthorized);
            }

            var address = await _addressRepository.GetDefaultAddressAsync(userId);

            if (address == null)
            {
                return ServiceResponse<GetAddressDto>.Fail("Bạn chưa có địa chỉ mặc định.", HttpStatusCode.NotFound);
            }

            var dto = _mapper.Map<GetAddressDto>(address);
            return ServiceResponse<GetAddressDto>.Success(dto);
        }

        // ✅ New: Helper method - Validate Vietnamese phone number
        private bool IsValidVietnamesePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return false;
            }

            // Remove spaces and dashes
            phoneNumber = phoneNumber.Replace(" ", "").Replace("-", "");

            // Vietnamese phone number patterns:
            // - 10 digits starting with 0: 0xxxxxxxxx
            // - 11 digits starting with 0: 0xxxxxxxxxx
            // - International format: +84xxxxxxxxx or +84xxxxxxxxxx
            var pattern = @"^(0|\+84)[1-9][0-9]{8,9}$";
            return System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, pattern);
        }
    }
}