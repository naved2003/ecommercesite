using Microsoft.EntityFrameworkCore;
using ecommercesite.Models;

namespace ecommercesite.Data
{
    /// <summary>
    /// EF Core DbContext for the ecommercesite database.
    /// </summary>
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Register> Registers => Set<Register>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<DeliveryPerson> DeliveryPeople => Set<DeliveryPerson>();
        public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
        public DbSet<Payment> Payments => Set<Payment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -------------------------------------------------------------------
            // Customer
            // -------------------------------------------------------------------
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("Customers");
                entity.HasKey(c => c.CustId);
                entity.Property(c => c.CustId)
                      .HasMaxLength(20)
                      .IsRequired();
                entity.Property(c => c.CustName)
                      .HasMaxLength(100)
                      .IsRequired();
                entity.Property(c => c.CustAddress)
                      .HasMaxLength(250);
                entity.HasIndex(c => c.CustName);
            });

            // -------------------------------------------------------------------
            // Register (user accounts)
            // -------------------------------------------------------------------
            modelBuilder.Entity<Register>(entity =>
            {
                entity.ToTable("Registers");
                entity.HasKey(r => r.UserId);
                entity.Property(r => r.UserId)
                      .HasMaxLength(36)
                      .IsRequired();
                entity.Property(r => r.FirstName).HasMaxLength(50).IsRequired();
                entity.Property(r => r.LastName).HasMaxLength(50).IsRequired();
                entity.Property(r => r.Email).HasMaxLength(150).IsRequired();
                entity.Property(r => r.Password).IsRequired();
                entity.Property(r => r.PhoneNumber).HasMaxLength(20).IsRequired();
                entity.Property(r => r.Role).HasMaxLength(20).IsRequired();
                entity.HasIndex(r => r.Email).IsUnique();
            });

            // -------------------------------------------------------------------
            // ProductCategory
            // -------------------------------------------------------------------
            modelBuilder.Entity<ProductCategory>(entity =>
            {
                entity.ToTable("ProductCategories");
                entity.HasKey(c => c.CategoryId);
                entity.Property(c => c.CategoryName)
                      .HasMaxLength(100)
                      .IsRequired();
                entity.Property(c => c.Description).HasMaxLength(500);
                entity.HasIndex(c => c.CategoryName).IsUnique();
                entity.HasMany(c => c.Products)
                      .WithOne(p => p.ProductCategory!)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // -------------------------------------------------------------------
            // Product
            // -------------------------------------------------------------------
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Products");
                entity.HasKey(p => p.ProductId);
                entity.Property(p => p.ProductName).HasMaxLength(150).IsRequired();
                entity.Property(p => p.Price).HasColumnType("decimal(18,2)").IsRequired();
                entity.HasIndex(p => p.CategoryId);
                entity.HasIndex(p => p.ProductName);
            });

            // -------------------------------------------------------------------
            // Order
            // -------------------------------------------------------------------
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");
                entity.HasKey(o => o.OrderId);
                // CustId is now optional — customer-placed orders use
                // RegisterId instead. Keep the FK but make the column nullable.
                entity.Property(o => o.CustId).HasMaxLength(20);
                entity.Property(o => o.RegisterId).HasMaxLength(36);
                entity.Property(o => o.Status).HasMaxLength(20).IsRequired();
                entity.Property(o => o.ShippingAddress).HasMaxLength(500);
                entity.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(o => o.OrderDate).HasColumnType("datetime2");
                entity.Property(o => o.CreatedAt).HasColumnType("datetime2");
                entity.Property(o => o.DeliveryStatus).HasMaxLength(20);
                entity.Property(o => o.DeliveredAt).HasColumnType("datetime2");

                entity.HasOne(o => o.Customer!)
                      .WithMany()
                      .HasForeignKey(o => o.CustId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Nullable FK to Register (user account) for customer-placed orders.
                entity.HasOne(o => o.Register!)
                      .WithMany()
                      .HasForeignKey(o => o.RegisterId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Nullable FK — an order may have no delivery person assigned.
                entity.HasOne(o => o.DeliveryPerson!)
                      .WithMany(d => d.AssignedOrders)
                      .HasForeignKey(o => o.DeliveryPersonId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(o => o.CustId);
                entity.HasIndex(o => o.RegisterId);
                entity.HasIndex(o => o.OrderDate);
                entity.HasIndex(o => o.Status);
                entity.HasIndex(o => o.DeliveryPersonId);
                entity.HasIndex(o => o.DeliveryStatus);
            });

            // -------------------------------------------------------------------
            // OrderItem
            // -------------------------------------------------------------------
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("OrderItems");
                entity.HasKey(i => i.OrderItemId);
                entity.Property(i => i.ProductName).HasMaxLength(150).IsRequired();
                entity.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(i => i.Quantity).IsRequired();

                entity.HasOne(i => i.Order!)
                      .WithMany(o => o.Items)
                      .HasForeignKey(i => i.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(i => i.Product!)
                      .WithMany()
                      .HasForeignKey(i => i.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(i => i.OrderId);
                entity.HasIndex(i => i.ProductId);
            });

            // -------------------------------------------------------------------
            // DeliveryPerson
            // -------------------------------------------------------------------
            modelBuilder.Entity<DeliveryPerson>(entity =>
            {
                entity.ToTable("DeliveryPeople");
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Name).HasMaxLength(100).IsRequired();
                entity.Property(d => d.Phone).HasMaxLength(20).IsRequired();
                entity.Property(d => d.Email).HasMaxLength(150);
                entity.Property(d => d.VehicleNumber).HasMaxLength(50);
                entity.Property(d => d.Status).HasMaxLength(20).IsRequired();
                entity.Property(d => d.AssignedArea).HasMaxLength(100);
                entity.Property(d => d.CreatedAt).HasColumnType("datetime2");
                entity.Property(d => d.UpdatedAt).HasColumnType("datetime2");

                entity.HasIndex(d => d.Phone).IsUnique();
                entity.HasIndex(d => d.Status);
            });

            // -------------------------------------------------------------------
            // ExternalLogin
            // -------------------------------------------------------------------
            modelBuilder.Entity<ExternalLogin>(entity =>
            {
                entity.ToTable("ExternalLogins");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).HasMaxLength(36).IsRequired();
                entity.Property(e => e.Provider).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ProviderKey).HasMaxLength(200).IsRequired();
                entity.Property(e => e.ProviderEmail).HasMaxLength(150);
                entity.Property(e => e.ProviderDisplayName).HasMaxLength(200);
                entity.Property(e => e.LinkedAt).HasColumnType("datetime2");

                // One (provider, providerKey) pair → one user. Prevents
                // the same Google account from being linked to two local
                // accounts.
                entity.HasIndex(e => new { e.Provider, e.ProviderKey }).IsUnique();
                entity.HasIndex(e => e.UserId);

                entity.HasOne(e => e.User!)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // -------------------------------------------------------------------
            // Payment
            // -------------------------------------------------------------------
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("Payments");
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
                entity.Property(p => p.Method).HasMaxLength(20).IsRequired();
                entity.Property(p => p.Status).HasMaxLength(20).IsRequired();
                entity.Property(p => p.TransactionId).HasMaxLength(64);
                entity.Property(p => p.CardLast4).HasMaxLength(4);
                entity.Property(p => p.CardBrand).HasMaxLength(20);
                entity.Property(p => p.FailureReason).HasMaxLength(200);
                entity.Property(p => p.CreatedAt).HasColumnType("datetime2");
                entity.Property(p => p.PaidAt).HasColumnType("datetime2");

                entity.HasOne(p => p.Order!)
                      .WithMany()
                      .HasForeignKey(p => p.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(p => p.OrderId);
                entity.HasIndex(p => p.Status);
            });
        }
    }
}