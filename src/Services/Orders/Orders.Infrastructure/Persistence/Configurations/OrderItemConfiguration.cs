using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductId).IsRequired();

        builder.Property(i => i.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.UnitPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.Quantity).IsRequired();

        // LineTotal é calculado em memória (UnitPrice * Quantity), não
        // uma coluna do banco.
        builder.Ignore(i => i.LineTotal);
    }
}
