using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Domain.Repositories;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;

namespace eCommerceApp.Infrastructure.Repositories
{
    /// <summary>
    /// Unit of Work implementation để quản lý transactions và repositories
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;

        // Repositories (lazy initialization)
        private IConversationRepository? _conversations;
        private IMessageRepository? _messages;
        private IOrderRepository? _orders;
        private IPaymentRepository? _payments;
        private ISettlementRepository? _settlements; // ✅ New
        private IProductRepository? _products;
        private IShopRepository? _shops;
        private IAddressRepository? _addresses;
        private ICartRepository? _carts;
        private IGlobalCategoryRepository? _globalCategories;
        private IShopCategoryRepository? _shopCategories;
        private IProductImageRepository? _productImages;
        private ISellerBalanceRepository? _sellerBalances; // ✅ New
        private IReviewRepository? _reviews; // ✅ New
        private IFeaturedEventRepository? _featuredEvents; // ✅ New
        private IFeaturedRankingRepository? _featuredRankings; // ✅ New

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        // Repositories properties
        public IConversationRepository Conversations =>
            _conversations ??= new ConversationRepository(_context);

        public IMessageRepository Messages =>
            _messages ??= new MessageRepository(_context);

        public IOrderRepository Orders =>
            _orders ??= new OrderRepository(_context);

        public IPaymentRepository Payments =>
            _payments ??= new PaymentRepository(_context);

        public ISettlementRepository Settlements =>
            _settlements ??= new SettlementRepository(_context); // ✅ New

        public IProductRepository Products =>
            _products ??= new ProductRepository(_context);

        public IShopRepository Shops =>
            _shops ??= new ShopRepository(_context);

        public IAddressRepository Addresses =>
            _addresses ??= new AddressRepository(_context);

        public ICartRepository Carts =>
            _carts ??= new CartRepository(_context);

        public IGlobalCategoryRepository GlobalCategories =>
            _globalCategories ??= new GlobalCategoryRepository(_context);

        public IShopCategoryRepository ShopCategories =>
            _shopCategories ??= new ShopCategoryRepository(_context);

        public IProductImageRepository ProductImages =>
            _productImages ??= new ProductImageRepository(_context);

        public ISellerBalanceRepository SellerBalances =>
            _sellerBalances ??= new SellerBalanceRepository(_context); // ✅ New

        public IReviewRepository Reviews =>
            _reviews ??= new ReviewRepository(_context); // ✅ New

        public IFeaturedEventRepository FeaturedEvents =>
            _featuredEvents ??= new FeaturedEventRepository(_context); // ✅ New

        public IFeaturedRankingRepository FeaturedRankings =>
            _featuredRankings ??= new FeaturedRankingRepository(_context); // ✅ New

        // Transaction management
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // ✅ Fix: Khi có transaction active, SaveChangesAsync() sẽ không sử dụng execution strategy retry
            // vì execution strategy với retry không tương thích với transactions
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("Transaction already started.");
            }
            
            // ✅ Fix: BeginTransactionAsync() tự động bypass execution strategy
            // Không wrap trong execution strategy vì nó sẽ gây xung đột
            // Khi có transaction active, EF Core sẽ tự động disable retry cho tất cả operations
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction to commit.");
            }

            try
            {
                await _transaction.CommitAsync(cancellationToken);
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction to rollback.");
            }

            try
            {
                await _transaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        /// <summary>
        /// ✅ Execute operation within a transaction wrapped in execution strategy
        /// This ensures compatibility with EnableRetryOnFailure() by wrapping
        /// the entire transaction (Begin, operations, Commit) in execution strategy
        /// </summary>
        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            // Get execution strategy from DbContext
            var strategy = _context.Database.CreateExecutionStrategy();
            
            // Wrap entire transaction in execution strategy
            return await strategy.ExecuteAsync(async () =>
            {
                // ✅ Fix: Ensure transaction is clean before starting (for retry scenarios)
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
                
                // Begin transaction directly from DbContext inside execution strategy
                // Sử dụng transaction trực tiếp để tránh xung đột với execution strategy retry
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Execute the operation
                    var result = await operation();
                    
                    // Save changes
                    await _context.SaveChangesAsync(cancellationToken);
                    
                    // Commit transaction
                    await transaction.CommitAsync(cancellationToken);
                    
                    return result;
                }
                catch
                {
                    // Rollback on any error
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}

