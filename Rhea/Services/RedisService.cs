using System.Text.Json;
using Rhea.Models;
using StackExchange.Redis;
using StackExchange.Redis.KeyspaceIsolation;

namespace Rhea.Services;

public class RedisService
{
    private readonly ConnectionMultiplexer client;
    private readonly string prefix;

    public RedisService()
    {
        var connectionUri = Environment.GetEnvironmentVariable("REDIS_URI");
        ArgumentException.ThrowIfNullOrEmpty(connectionUri);

        client = ConnectionMultiplexer.Connect(connectionUri);
        prefix = Environment.GetEnvironmentVariable("REDIS_PREFIX") ?? "rhea:";
    }

    private IDatabase GetDatabase() => client.GetDatabase().WithKeyPrefix(prefix);

    public List<EnrichedTrack> GetQueue(ulong ID)
    {
        var result = GetDatabase().StringGetWithExpiry($"queue:{ID}");
        return result.Value.IsNullOrEmpty
            ? new List<EnrichedTrack>()
            : JsonSerializer.Deserialize<List<EnrichedTrack>>(result.Value.ToString())!;
    }

    public async Task<List<EnrichedTrack>> GetQueueAsync(ulong ID)
    {
        var result = await GetDatabase().StringGetWithExpiryAsync($"queue:{ID}");
        return result.Value.IsNullOrEmpty
            ? new List<EnrichedTrack>()
            : JsonSerializer.Deserialize<List<EnrichedTrack>>(result.Value.ToString())!;
    }

    public async Task SetQueueAsync(ulong ID, List<EnrichedTrack> queue)
    {
        await GetDatabase().StringSetAsync($"queue:{ID}", JsonSerializer.Serialize(queue), TimeSpan.FromDays(1));
    }

    public async Task ClearQueueAsync(ulong ID)
    {
        await GetDatabase().KeyDeleteAsync($"queue:{ID}");
    }

    public async Task<PlayingTrack?> GetPlayingAsync(ulong ID)
    {
        var result = await GetDatabase().StringGetWithExpiryAsync($"playing:{ID}");
        return result.Value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<PlayingTrack>(result.Value.ToString());
    }

    public async Task SetPlayingAsync(ulong ID, PlayingTrack track)
    {
        await GetDatabase().StringSetAsync($"playing:{ID}", JsonSerializer.Serialize(track), TimeSpan.FromDays(1));
    }

    public async Task DeletePlayingAsync(ulong ID)
    {
        await GetDatabase().KeyDeleteAsync($"playing:{ID}");
    }

    public async Task<List<ulong>> GetPlayers()
    {
        var db = GetDatabase();
        var schemas = new List<ulong>();
        var cursor = 0;
        do
        {
            RedisResult redisResult = await db.ExecuteAsync("SCAN", cursor.ToString(), "MATCH", $"{prefix}playing*");
            var innerResult = (RedisResult[])redisResult!;

            cursor = int.Parse(innerResult[0].ToString());
            schemas.AddRange(((string[])innerResult[1]!).Select(key => ulong.Parse(key.Split(':').Last())));
        } while (cursor != 0);

        return schemas;
    }
}