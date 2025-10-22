using System;
using Microsoft.EntityFrameworkCore;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using eCommerceApp.Domain.Entities.Identity;

namespace eCommerceApp.Infrastructure.Data
{
    // Kế thừa IdentityDbContext với User và IdentityRole
    public class AppDbContext : IdentityDbContext<User, IdentityRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // Bổ sung DbSet cho Entity mới: Conversation
        public DbSet<Shop> Shops { get; set; } = null!;
        public DbSet<GlobalCategory> GlobalCategoris { get; set; } = null!;
        public DbSet<ShopCategory> ShopCategories { get; set; }
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<ProductImage> ProductImages { get; set; } = null!;
        public DbSet<Promotion> Promotions { get; set; } = null!;
        public DbSet<Cart> Carts { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<Address> Addresses { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<Conversation> Conversations { get; set; } = null!; // ✅ Đã sửa
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<ViolationReport> ViolationReports { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table names (map User to AspNetUsers)
            modelBuilder.Entity<User>().ToTable("AspNetUsers");

            // ---------- Keys & Composite Keys ----------
            modelBuilder.Entity<CartItem>()
                .HasKey(ci => new { ci.CartId, ci.ProductId });

            modelBuilder.Entity<OrderItem>()
                .HasKey(oi => new { oi.OrderId, oi.ProductId });

            // ---------- Decimal precision ----------
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            // Độ chính xác cho thuộc tính mới trong Product
            modelBuilder.Entity<Product>()
                .Property(p => p.AverageRating)
                .HasColumnType("float");

            modelBuilder.Entity<Promotion>()
                .Property(p => p.Discount)
                .HasColumnType("decimal(18,2)");

            // Độ chính xác cho thuộc tính mới trong Promotion
            modelBuilder.Entity<Promotion>()
                .Property(p => p.MinOrderAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

            // Độ chính xác cho thuộc tính mới trong Order
            modelBuilder.Entity<Order>()
                .Property(o => o.ShippingFee)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Order>()
                .Property(o => o.DiscountAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.PriceAtPurchase)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            // ---------- Relationships ----------
            // User - Shop (Seller)
            modelBuilder.Entity<Shop>()
                .HasOne(s => s.Seller)
                .WithMany(u => u.Shops)
                .HasForeignKey(s => s.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Make SellerId unique (one seller -> one shop) if that's required by diagram
            modelBuilder.Entity<Shop>()
                .HasIndex(s => s.SellerId)
                .IsUnique();

            // Shop - Product
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Shop)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.ShopId)
                .OnDelete(DeleteBehavior.Restrict);

            // Category - Product
            modelBuilder.Entity<Product>()
                .HasOne(p => p.GlobalCategory)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.GlobalCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShopCategory>()
                .HasOne(sc => sc.Shop)
                .WithMany() 
                .HasForeignKey(sc => sc.ShopId)
                .OnDelete(DeleteBehavior.Cascade); 

            modelBuilder.Entity<ShopCategory>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            // Product - ProductImage
            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Promotion -> Shop / Product (nullable)
            modelBuilder.Entity<Promotion>()
                .HasOne(pr => pr.Shop)
                .WithMany(s => s.Promotions)
                .HasForeignKey(pr => pr.ShopId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Promotion>()
                .HasOne(pr => pr.Product)
                .WithMany(p => p.Promotions)
                .HasForeignKey(pr => pr.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cart - CartItem
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Address - User
            modelBuilder.Entity<Address>()
                .HasOne(a => a.User)
                .WithMany(u => u.Addresses)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Order - OrderItem
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order - Payment (1:1)
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                // ❌ SỬA LỖI CS1061: Thay thế .WithOne(o => o.Payment) bằng .WithOne() vì Order không còn Navigation Property
                .WithOne()
                .HasForeignKey<Payment>(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Order - Shop (Bổ sung cho Multi-shop Order)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Shop)
                .WithMany()
                .HasForeignKey(o => o.ShopId)
                .OnDelete(DeleteBehavior.Restrict);

            // Review -> Product / User
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ---------- Mối quan hệ Chat mới (Giai đoạn 8) ----------
            // Conversation
            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.User1)
                .WithMany()
                .HasForeignKey(c => c.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.User2)
                .WithMany()
                .HasForeignKey(c => c.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Message -> Conversation / Sender
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            // --------------------------------------------------------

            // ViolationReport -> Product / Reporter / Admin
            modelBuilder.Entity<ViolationReport>()
                .HasOne(v => v.Product)
                .WithMany(p => p.ViolationReports)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ViolationReport>()
                .HasOne(v => v.Reporter)
                .WithMany(u => u.ViolationReports)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ViolationReport>()
                .HasOne(v => v.Admin)
                .WithMany()
                .HasForeignKey(v => v.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            // RefreshToken -> User
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cart -> User
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Customer)
                .WithMany(u => u.Carts)
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Order -> Address / Customer
            // ❌ SỬA LỖI CS0103: Sửa 'modelModelBuilder' thành 'modelBuilder'
            modelBuilder.Entity<Order>()
                .HasOne(o => o.ShippingAddress)
                .WithMany()
                .HasForeignKey(o => o.AddressId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // ---------- Global Query Filters (soft-delete) ----------
            modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
            modelBuilder.Entity<Shop>().HasQueryFilter(s => !s.IsDeleted);
            modelBuilder.Entity<GlobalCategory>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<ShopCategory>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<Promotion>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<Address>().HasQueryFilter(a => !a.IsDeleted);
            modelBuilder.Entity<Order>().HasQueryFilter(o => !o.IsDeleted);
            modelBuilder.Entity<Conversation>().HasQueryFilter(c => !c.IsDeleted);

            // ---------- Indexes (optional helpful ones) ----------
            modelBuilder.Entity<User>()
                .HasIndex(u => u.NormalizedUserName)
                .IsUnique(false);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.NormalizedEmail)
                .IsUnique(false);

            modelBuilder.Entity<Promotion>()
                .HasIndex(p => p.Code)
                .IsUnique();
        }
    }
}