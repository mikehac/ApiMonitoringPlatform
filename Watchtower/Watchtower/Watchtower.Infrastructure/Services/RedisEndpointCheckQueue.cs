using StackExchange.Redis;
using Watchtower.Application.Abstractions;

namespace Watchtower.Infrastructure.Services;

public class RedisEndpointCheckQueue : IEndpointCheckQueue
{
    private const string QueueKey = "watchtower:check-queue";

    private readonly IConnectionMultiplexer _redis;

    public RedisEndpointCheckQueue(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task SeedAsync(IEnumerable<Guid> endpointIds, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var id in endpointIds)
            await db.SortedSetAddAsync(QueueKey, id.ToString(), now, SortedSetWhen.NotExists);
    }

    public async Task EnqueueAsync(Guid endpointId, int delaySeconds, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var score = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + delaySeconds;
        await db.SortedSetAddAsync(QueueKey, endpointId.ToString(), score);
    }

    public async Task<IReadOnlyList<Guid>> DequeueDueAsync(int maxCount = 50, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Atomically fetch and remove all entries with score <= now
        const string script = """
            local due = redis.call('ZRANGEBYSCORE', KEYS[1], '-inf', ARGV[1], 'LIMIT', 0, ARGV[2])
            if #due > 0 then
                redis.call('ZREM', KEYS[1], unpack(due))
            end
            return due
            """;

        var result = await db.ScriptEvaluateAsync(
            script,
            new RedisKey[] { QueueKey },
            new RedisValue[] { now, maxCount });

        if (result.IsNull)
            return [];

        return ((RedisValue[])result!)
            .Select(v => Guid.Parse(v.ToString()))
            .ToList();
    }

    public async Task<IReadOnlySet<Guid>> GetAllQueuedAsync(CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var all = await db.SortedSetRangeByRankAsync(QueueKey);
        return all.Select(v => Guid.Parse(v.ToString())).ToHashSet();
    }

    public async Task RemoveAsync(IEnumerable<Guid> endpointIds, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var values = endpointIds.Select(id => (RedisValue)id.ToString()).ToArray();
        if (values.Length > 0)
            await db.SortedSetRemoveAsync(QueueKey, values);
    }
}
