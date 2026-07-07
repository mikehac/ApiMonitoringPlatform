using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Watchtower.Application;
using Watchtower.Application.Abstractions;
using Watchtower.Infrastructure.Persistence;
using Watchtower.Infrastructure.Services;

namespace Watchtower.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection string is not configured.");

        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddStackExchangeRedisCache(options =>
            options.Configuration = redisConnectionString);

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailService, EmailService>();

        services.AddSingleton<IEndpointCheckQueue, RedisEndpointCheckQueue>();
        services.AddSingleton<IMonitoringEventPublisher, RedisMonitoringEventPublisher>();

        services.AddScoped<IAlertingService, AlertingService>();

        services.Configure<AlertingOptions>(
            configuration.GetSection(AlertingOptions.SectionName));

        return services;
    }
}
