using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Watchtower.Domain.Entities;

namespace Watchtower.Infrastructure.Persistence.Configurations;

public class MonitoredEndpointConfiguration : IEntityTypeConfiguration<MonitoredEndpoint>
{
    public void Configure(EntityTypeBuilder<MonitoredEndpoint> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Url).HasMaxLength(2048).IsRequired();
        builder.Property(e => e.HttpMethod).HasMaxLength(10).IsRequired();
        builder.Property(e => e.ExpectedBodyContains).HasMaxLength(500);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(e => e.OwnerId);
        builder.HasIndex(e => new { e.IsActive, e.LastCheckedAt });

        builder.HasMany(e => e.CheckResults)
               .WithOne(r => r.Endpoint)
               .HasForeignKey(r => r.EndpointId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Alerts)
               .WithOne(a => a.Endpoint)
               .HasForeignKey(a => a.EndpointId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
