using Medzo.PurchasingSupplier.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace Medzo.PurchasingSupplier.Infrastructure.Persistence;

public sealed class PurchasingSupplierDbContext(DbContextOptions<PurchasingSupplierDbContext> options) : DbContext(options)
{
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        var supplier = builder.Entity<Supplier>();
        supplier.ToTable("suppliers"); supplier.HasKey(x => x.Id); supplier.Property(x => x.Id).ValueGeneratedNever();
        supplier.Property(x => x.Name).HasMaxLength(Supplier.MaxNameLength).IsRequired(); supplier.Property(x => x.NormalizedName).HasMaxLength(Supplier.MaxNameLength).IsRequired();
        supplier.Property(x => x.ContactName).HasMaxLength(Supplier.MaxContactNameLength).IsRequired(); supplier.Property(x => x.Email).HasMaxLength(Supplier.MaxEmailLength).IsRequired();
        supplier.Property(x => x.NormalizedEmail).HasMaxLength(Supplier.MaxEmailLength).IsRequired(); supplier.Property(x => x.Phone).HasMaxLength(Supplier.MaxPhoneLength).IsRequired(); supplier.Property(x => x.Address).HasMaxLength(Supplier.MaxAddressLength);
        supplier.HasIndex(x => x.NormalizedName); supplier.HasIndex(x => x.NormalizedEmail);
    }
}
