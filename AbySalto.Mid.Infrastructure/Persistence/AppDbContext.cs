using AbySalto.Mid.Domain.Entities;
using AbySalto.Mid.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AbySalto.Mid.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Basket> Basket => Set<Basket>();
    public DbSet<BasketItem> BasketItem => Set<BasketItem>();
    public DbSet<Favorite> Favorite => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // TODO: if there are many entities, consider using IEntityTypeConfiguration<TEntity> 
        // https://learn.microsoft.com/en-us/ef/core/modeling/#grouping-configuration

        // ----------------------------------------------------------------------------------------
        builder.Entity<Basket>(entity =>
        {
            entity.HasKey(basket => basket.Id);

            entity.Property(basket => basket.Id)
                .ValueGeneratedNever();

            entity.HasIndex(basket => basket.UserId)
                .IsUnique();

            entity.HasOne<ApplicationUser>()
                .WithOne()
                .HasForeignKey<Basket>(basket => basket.UserId);

            entity.HasMany(basket => basket.Items)
                .WithOne()
                .HasForeignKey(item => item.BasketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(basket => basket.Items)
                .HasField("_items")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // ----------------------------------------------------------------------------------------
        builder.Entity<BasketItem>(entity =>
        {
            entity.HasKey(item => item.Id);

            entity.Property(item => item.Id)
                .ValueGeneratedNever();

            entity.HasIndex(item => new
            {
                item.BasketId,
                item.ProductId
            })
            .IsUnique()
            .HasDatabaseName("UQ_BasketItem_BasketId_ProductId");

            entity.Property(item => item.ProductTitle)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(item => item.UnitPrice)
                .HasPrecision(18, 2);
        });

        // ----------------------------------------------------------------------------------------
        builder.Entity<Favorite>(entity =>
        {
            entity.HasKey(item => item.Id);

            entity.Property(item => item.Id)
                .ValueGeneratedNever();

            entity.HasIndex(item => new
            {
                item.UserId,
                item.ProductId
            })
            .IsUnique()
            .HasDatabaseName("UQ_Favorite_UserId_ProductId");

            entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(favorite => favorite.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        });
    }
}