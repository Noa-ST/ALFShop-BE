﻿using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Address;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IAddressService
    {
        // POST /api/Address/create
        Task<ServiceResponse> CreateAddressAsync(string userId, CreateAddress dto);

        // PUT /api/Address/update/{id}
        Task<ServiceResponse> UpdateAddressAsync(string userId, Guid addressId, UpdateAddress dto);

        // DELETE /api/Address/delete/{id} (Soft Delete)
        Task<ServiceResponse> DeleteAddressAsync(string userId, Guid addressId);

        // GET /api/Address/list
        Task<ServiceResponse<IEnumerable<GetAddressDto>>> GetUserAddressesAsync(string userId);
    }
}