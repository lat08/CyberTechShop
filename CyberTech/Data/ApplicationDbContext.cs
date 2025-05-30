using Microsoft.EntityFrameworkCore;
using CyberTech.Models;

namespace CyberTech.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserAuthMethod> UserAuthMethods { get; set; }
        public DbSet<Rank> Ranks { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }

        public DbSet<CategoryAttributes> CategoryAttributes { get; set; }
        public DbSet<Subcategory> Subcategories { get; set; }
        public DbSet<SubSubcategory> SubSubcategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductAttribute> ProductAttributes { get; set; }
        public DbSet<AttributeValue> AttributeValues { get; set; }
        public DbSet<ProductAttributeValue> ProductAttributeValues { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<VoucherProducts> VoucherProducts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure table names for all entities
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<UserAuthMethod>().ToTable("UserAuthMethods");
            modelBuilder.Entity<Rank>().ToTable("Ranks");
            modelBuilder.Entity<UserAddress>().ToTable("UserAddress");
            modelBuilder.Entity<PasswordResetToken>().ToTable("PasswordResetTokens");
            modelBuilder.Entity<Category>().ToTable("Category");
            modelBuilder.Entity<CategoryAttributes>().ToTable("CategoryAttributes");
            modelBuilder.Entity<Subcategory>().ToTable("Subcategory");
            modelBuilder.Entity<SubSubcategory>().ToTable("SubSubcategory");
            modelBuilder.Entity<Product>().ToTable("Product");
            modelBuilder.Entity<ProductImage>().ToTable("ProductImage");
            modelBuilder.Entity<ProductAttribute>().ToTable("ProductAttribute");
            modelBuilder.Entity<AttributeValue>().ToTable("AttributeValue");
            modelBuilder.Entity<ProductAttributeValue>().ToTable("ProductAttributeValue");
            modelBuilder.Entity<Wishlist>().ToTable("Wishlist");
            modelBuilder.Entity<Cart>().ToTable("Cart");
            modelBuilder.Entity<CartItem>().ToTable("CartItem");
            modelBuilder.Entity<Voucher>().ToTable("Vouchers");
            modelBuilder.Entity<Order>().ToTable("Orders");
            modelBuilder.Entity<OrderItem>().ToTable("OrderItems");
            modelBuilder.Entity<Payment>().ToTable("Payment");
            modelBuilder.Entity<Review>().ToTable("Review");
            modelBuilder.Entity<WishlistItem>().ToTable("WishlistItem");

            // Configure VoucherProducts composite key
            modelBuilder.Entity<VoucherProducts>()
                .HasKey(vp => new { vp.VoucherID, vp.ProductID });

            modelBuilder.Entity<VoucherProducts>()
                .HasOne(vp => vp.Voucher)
                .WithMany(v => v.VoucherProducts)
                .HasForeignKey(vp => vp.VoucherID);

            modelBuilder.Entity<VoucherProducts>()
                .HasOne(vp => vp.Product)
                .WithMany(p => p.VoucherProducts)
                .HasForeignKey(vp => vp.ProductID);

            // Cấu hình khóa chính cho bảng ProductAttributeValue
            modelBuilder.Entity<ProductAttributeValue>()
                .HasKey(pav => new { pav.ProductID, pav.ValueID });

            // Cấu hình mối quan hệ nhiều-nhiều giữa Product và AttributeValue
            modelBuilder.Entity<ProductAttributeValue>()
                .HasOne(pav => pav.AttributeValue)
                .WithMany(av => av.ProductAttributeValues)
                .HasForeignKey(pav => pav.ValueID);


            // Cấu hình các trường tính toán
            modelBuilder.Entity<Order>().ToTable("Orders");
            modelBuilder.Entity<Order>()
                .HasKey(o => o.OrderID);
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.UserID);
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderID);
            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasDefaultValue("Pending");

            // Thêm cấu hình cho OrderItem
            modelBuilder.Entity<OrderItem>().ToTable("OrderItem");
            modelBuilder.Entity<OrderItem>()
                .HasKey(oi => oi.OrderItemID);
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductID);
        }
    }
}