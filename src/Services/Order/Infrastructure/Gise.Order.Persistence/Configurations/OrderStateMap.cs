using Gise.Order.Application.Sagas;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Order.Persistence.Configurations;

// Saga durumu tablosu; birincil anahtar CorrelationId (siparişin ID'si). Biten saga'nın satırı silinir.
public sealed class OrderStateMap : SagaClassMap<OrderState>
{
    protected override void Configure(EntityTypeBuilder<OrderState> entity, ModelBuilder model)
    {
        entity.ToTable("order_sagas");
        entity.Property(x => x.CurrentState).HasMaxLength(64).IsRequired();
        entity.Property(x => x.CustomerId).HasMaxLength(64).IsRequired();
        entity.Property(x => x.Amount).HasPrecision(10, 2);
    }
}
