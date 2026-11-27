using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<OrderRow> Orders => Set<OrderRow>();

    public DbSet<OrderLineRow> OrderLines =>
        Set<OrderLineRow>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OrderRow>(entity =>
        {
            entity.ToTable("Orders");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
    .IsRequired()
    .HasMaxLength(36)
    .UseCollation("NOCASE")
    .ValueGeneratedNever();

            entity.Property(x => x.CustomerId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.Property(x => x.Status)
                .IsRequired();

            entity.HasMany(x => x.Lines)
                .WithOne(x => x.Order)
                .HasForeignKey(x => x.OrderId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderLineRow>(entity =>
        {
            entity.ToTable("OrderLines");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.OrderId)
    .IsRequired()
    .HasMaxLength(36)
    .UseCollation("NOCASE");

            entity.Property(x => x.ProductId)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Price)
                .IsRequired();

            entity.Property(x => x.Quantity)
                .IsRequired();

            entity.Property(x => x.Position)
                .IsRequired();
        });
    }
}