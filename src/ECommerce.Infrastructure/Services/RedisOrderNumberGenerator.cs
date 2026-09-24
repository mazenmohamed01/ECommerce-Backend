using ECommerce.Domain.Interfaces;
using StackExchange.Redis;

namespace ECommerce.Infrastructure.Services;

public sealed class RedisOrderNumberGenerator : IOrderNumberGenerator
{
    private readonly IConnectionMultiplexer _redis;
    private const string OrderSequenceKey = "seq:order_number";

    public RedisOrderNumberGenerator(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        long nextId;
        try
        {
            // Only attempt if redis claims to be connected, otherwise skip to avoid 5-second timeout
            if (_redis.IsConnected)
            {
                var db = _redis.GetDatabase();
                nextId = await db.StringIncrementAsync(OrderSequenceKey);
            }
            else
            {
                nextId = Random.Shared.Next(100000, 999999);
            }
        }
        catch
        {
            // Fallback for development if Redis fails
            nextId = Random.Shared.Next(100000, 999999);
        }
        
        // Format: ORD-YYYYMMDD-XXXXXX
        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        var sequence = nextId.ToString("D6"); // Pad to 6 digits

        return $"ORD-{datePrefix}-{sequence}";
    }
}
