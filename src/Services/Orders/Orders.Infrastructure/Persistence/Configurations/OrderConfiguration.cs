using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.UserId).IsRequired();

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.CreatedAt).IsRequired();

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Order.Items só expõe um getter (IReadOnlyCollection<OrderItem>);
        // dizemos ao EF Core para ler/escrever direto no campo _items,
        // preservando o encapsulamento do agregado.
        builder.Metadata.FindNavigation(nameof(Order.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.Status);
    }
}
