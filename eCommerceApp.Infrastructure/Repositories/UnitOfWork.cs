using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Domain.Repositories;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

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
        private IProductRepository? _products;
        private IShopRepository? _shops;
        private IAddressRepository? _addresses;
        private ICartRepository? _carts;
        private IGlobalCategoryRepository? _globalCategories;
        private IShopCategoryRepository? _shopCategories;
        private IProductImageRepository? _productImages;

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

        // Transaction management
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("Transaction already started.");
            }
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

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}

