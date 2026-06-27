using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Watchtower.Domain.Entities;

namespace Watchtower.Infrastructure.Persistence.Configurations;

public class CheckResultConfiguration : IEntityTypeConfiguration<CheckResult>
{
    public void Configure(EntityTypeBuilder<CheckResult> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ErrorMessage).HasMaxLength(1000);

        // Time-series queries: filter by endpoint + recency
        builder.HasIndex(r => new { r.EndpointId, r.CheckedAt });
    }
}
