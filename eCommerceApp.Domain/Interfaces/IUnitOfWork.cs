using System;
using System.Threading;
using System.Threading.Tasks;
using eCommerceApp.Domain.Repositories;

namespace eCommerceApp.Domain.Interfaces
{
    /// <summary>
    /// Unit of Work pattern interface để quản lý transactions và repositories
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // Repositories (from Domain.Repositories)
        IConversationRepository Conversations { get; }
        IMessageRepository Messages { get; }
        IOrderRepository Orders { get; }
        IPaymentRepository Payments { get; }
        ISettlementRepository Settlements { get; } // ✅ New
        
        // Repositories (from Domain.Interfaces)
        IProductRepository Products { get; }
        IShopRepository Shops { get; }
        IAddressRepository Addresses { get; }
        ICartRepository Carts { get; }
        IGlobalCategoryRepository GlobalCategories { get; }
        IShopCategoryRepository ShopCategories { get; }
        IProductImageRepository ProductImages { get; }
        ISellerBalanceRepository SellerBalances { get; } // ✅ New

        // Transaction management
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
        
        // ✅ New: Execute transaction with execution strategy support
        // Wraps entire transaction logic in execution strategy to support retry
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
    }
}

