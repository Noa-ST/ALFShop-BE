using System;
using System.Threading.Tasks;

namespace eCommerceApp.Domain.Interfaces
{
	public interface IUnitOfWork : IDisposable
	{
		// Hàm này sẽ gọi context.SaveChangesAsync()
		Task<int> SaveChangesAsync();
	}
}