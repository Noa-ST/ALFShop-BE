using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eCommerceApp.Domain.Interfaces
{
    public interface IGeneric<IEntity> where IEntity : class
    {
        Task<IEnumerable<IEntity>> GetAllAsync();
        Task<IEntity> GetByIdAsync(Guid id);
        Task<int> AddAsync(IEntity entity);
        Task<int> UpdateAsync(IEntity entity);
        Task<int> DeleteAsync(Guid id);       
    }
}
