using Medzo.PurchasingSupplier.Domain.Suppliers;
using Medzo.PurchasingSupplier.Domain.PurchaseOrders;
using Microsoft.EntityFrameworkCore;

namespace Medzo.PurchasingSupplier.Infrastructure.Persistence;

public sealed class PurchasingSupplierDbContext(DbContextOptions<PurchasingSupplierDbContext> options) : DbContext(options)
{
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        var supplier = builder.Entity<Supplier>();
        supplier.ToTable("suppliers"); supplier.HasKey(x => x.Id); supplier.Property(x => x.Id).ValueGeneratedNever();
        supplier.Property(x => x.Name).HasMaxLength(Supplier.MaxNameLength).IsRequired(); supplier.Property(x => x.NormalizedName).HasMaxLength(Supplier.MaxNameLength).IsRequired();
        supplier.Property(x => x.ContactName).HasMaxLength(Supplier.MaxContactNameLength).IsRequired(); supplier.Property(x => x.Email).HasMaxLength(Supplier.MaxEmailLength).IsRequired();
        supplier.Property(x => x.NormalizedEmail).HasMaxLength(Supplier.MaxEmailLength).IsRequired(); supplier.Property(x => x.Phone).HasMaxLength(Supplier.MaxPhoneLength).IsRequired(); supplier.Property(x => x.Address).HasMaxLength(Supplier.MaxAddressLength);
        supplier.Property(x => x.IsActive).HasDefaultValue(true);
        supplier.HasIndex(x => x.NormalizedName); supplier.HasIndex(x => x.NormalizedEmail);

        var purchaseOrder = builder.Entity<PurchaseOrder>();
        purchaseOrder.ToTable("purchase_orders"); purchaseOrder.HasKey(x => x.Id); purchaseOrder.Property(x => x.Id).ValueGeneratedNever();
        purchaseOrder.Property(x => x.OrderNumber).HasMaxLength(30).IsRequired(); purchaseOrder.HasIndex(x => x.OrderNumber).IsUnique();
        purchaseOrder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        purchaseOrder.Property(x => x.ReceivedAtUtc);
        purchaseOrder.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        purchaseOrder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);

        var purchaseOrderItem = builder.Entity<PurchaseOrderItem>();
        purchaseOrderItem.ToTable("purchase_order_items"); purchaseOrderItem.HasKey(x => x.Id); purchaseOrderItem.Property(x => x.Id).ValueGeneratedNever();
        purchaseOrderItem.Property(x => x.MedicineName).HasMaxLength(200).IsRequired();
    }
}
