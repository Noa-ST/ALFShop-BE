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

public class AddressService : IAddressService
{
    private readonly IAddressRepository _addressRepository;
    private readonly IMapper _mapper;

    public AddressService(IAddressRepository addressRepository, IMapper mapper)
    {
        _addressRepository = addressRepository;
        _mapper = mapper;
    }

    // ✅ POST /api/Address/create
    public async Task<ServiceResponse> CreateAddressAsync(string userId, CreateAddress dto)
    {
        // 1. Nếu địa chỉ mới là mặc định, hủy đặt mặc định cho các địa chỉ cũ
        if (dto.IsDefault)
        {
            await _addressRepository.SetDefaultAddressAsync(userId, Guid.Empty); // Hàm này cần được triển khai để hủy đặt mặc định
        }

        // 2. Ánh xạ và gán UserId
        var address = _mapper.Map<Address>(dto);
        address.UserId = userId;
        address.CreatedAt = DateTime.UtcNow;

        // 3. Nếu là địa chỉ đầu tiên, tự động đặt làm mặc định
        var existingCount = (await _addressRepository.GetUserAddressesAsync(userId)).Count();
        if (existingCount == 0)
        {
            address.IsDefault = true;
        }

        await _addressRepository.AddAsync(address);
        return ServiceResponse.Success("Thêm địa chỉ giao hàng thành công.");
    }

    // ✅ PUT /api/Address/update/{id}
    public async Task<ServiceResponse> UpdateAddressAsync(string userId, Guid addressId, UpdateAddress dto)
    {
        var existingAddress = await _addressRepository.GetByIdAsync(addressId);

        // 1. Kiểm tra quyền sở hữu
        if (existingAddress == null || existingAddress.UserId != userId || existingAddress.IsDeleted)
        {
            return ServiceResponse.Fail("Không tìm thấy địa chỉ hoặc không có quyền truy cập.", HttpStatusCode.NotFound);
        }

        // 2. Xử lý logic IsDefault
        if (dto.IsDefault && !existingAddress.IsDefault)
        {
            await _addressRepository.SetDefaultAddressAsync(userId, addressId);
        }

        // 3. Ánh xạ và cập nhật
        _mapper.Map(dto, existingAddress);
        existingAddress.UpdatedAt = DateTime.UtcNow;

        await _addressRepository.UpdateAsync(existingAddress);
        return ServiceResponse.Success("Cập nhật địa chỉ thành công.");
    }

    // ✅ DELETE /api/Address/delete/{id}
    public async Task<ServiceResponse> DeleteAddressAsync(string userId, Guid addressId)
    {
        var existingAddress = await _addressRepository.GetByIdAsync(addressId);

        // 1. Kiểm tra quyền sở hữu
        if (existingAddress == null || existingAddress.UserId != userId || existingAddress.IsDeleted)
        {
            return ServiceResponse.Fail("Không tìm thấy địa chỉ hoặc không có quyền truy cập.", HttpStatusCode.NotFound);
        }

        // 2. Soft Delete
        existingAddress.IsDeleted = true;
        existingAddress.UpdatedAt = DateTime.UtcNow;
        await _addressRepository.UpdateAsync(existingAddress);

        // 3. Logic phụ: Nếu xóa địa chỉ mặc định, đặt địa chỉ khác làm mặc định (nếu có)
        if (existingAddress.IsDefault)
        {
            // Tạm thời bỏ qua, vì nó là logic phức tạp. Nên xử lý ở FE hoặc trong hàm riêng.
        }

        return ServiceResponse.Success("Xóa địa chỉ thành công.");
    }

    // ✅ GET /api/Address/list
    public async Task<ServiceResponse<IEnumerable<GetAddressDto>>> GetUserAddressesAsync(string userId)
    {
        var addresses = await _addressRepository.GetUserAddressesAsync(userId);
        var dtos = _mapper.Map<IEnumerable<GetAddressDto>>(addresses);

        return ServiceResponse<IEnumerable<GetAddressDto>>.Success(dtos);
    }
}