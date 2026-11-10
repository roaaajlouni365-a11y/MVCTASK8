using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CafeteriaSupplyTask8.Models;

namespace CafeteriaSupplyTask8.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<SupplyRequest> SupplyRequests => Set<SupplyRequest>();
    public DbSet<RequestItem> RequestItems => Set<RequestItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Keep the key lengths used by the original Identity template migration.
        builder.Entity<IdentityUserLogin<string>>().Property(x => x.LoginProvider).HasMaxLength(128);
        builder.Entity<IdentityUserLogin<string>>().Property(x => x.ProviderKey).HasMaxLength(128);
        builder.Entity<IdentityUserToken<string>>().Property(x => x.LoginProvider).HasMaxLength(128);
        builder.Entity<IdentityUserToken<string>>().Property(x => x.Name).HasMaxLength(128);
        builder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
        builder.Entity<SupplyRequest>().Property(r => r.TotalPrice).HasPrecision(18, 2);
        builder.Entity<RequestItem>().Property(i => i.Price).HasPrecision(18, 2);
        builder.Entity<SupplyRequest>().Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Entity<SupplyRequest>().HasOne(r => r.User).WithMany()
            .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<RequestItem>().HasOne(i => i.SupplyRequest).WithMany(r => r.Items)
            .HasForeignKey(i => i.SupplyRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<RequestItem>().HasOne(i => i.Product).WithMany()
            .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<RequestItem>().HasIndex(i => new { i.SupplyRequestId, i.ProductId }).IsUnique();
        builder.Entity<Product>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_Product_Quantity", "[Quantity] >= 0");
            t.HasCheckConstraint("CK_Product_Price", "[Price] > 0");
        });
        builder.Entity<RequestItem>().ToTable(t => t.HasCheckConstraint("CK_Item_Quantity", "[Quantity] > 0"));
    }
}
