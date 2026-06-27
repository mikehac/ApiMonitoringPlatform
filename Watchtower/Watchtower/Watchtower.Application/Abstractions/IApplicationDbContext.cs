using Microsoft.EntityFrameworkCore;
using Watchtower.Domain.Entities;

namespace Watchtower.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<MonitoredEndpoint> Endpoints { get; }
    DbSet<CheckResult> CheckResults { get; }
    DbSet<Alert> Alerts { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
