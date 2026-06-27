using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Watchtower.Domain.Entities;

namespace Watchtower.Infrastructure.Persistence.Configurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Reason).HasMaxLength(500).IsRequired();
        builder.Property(a => a.State).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(a => new { a.EndpointId, a.State });
    }
}
