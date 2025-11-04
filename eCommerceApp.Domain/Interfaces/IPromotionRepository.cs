using eCommerceApp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace eCommerceApp.Domain.Interfaces
{
    public interface IPromotionRepository
    {
        Task<Promotion> GetByIdAsync(Guid id);
        Task<Promotion> GetByCodeAsync(string code);
        Task<IEnumerable<Promotion>> GetActivePromotionsAsync();
        Task AddAsync(Promotion promotion);
        void Update(Promotion promotion);
        void Delete(Promotion promotion);
    }
}