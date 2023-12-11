using System.Collections;
using Lavalink4NET.Players.Queued;
using Rhea.Services;

namespace Rhea.Models;

public class TrackQueue(RedisService redis, ulong ID) : ITrackQueue
{
    public IEnumerator<ITrackQueueItem> GetEnumerator() => redis.GetQueue(ID).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => redis.GetQueue(ID).Count;

    public ITrackQueueItem this[int index] => redis.GetQueue(ID)[index];

    public bool Contains(ITrackQueueItem item) => redis.GetQueue(ID).Contains(item);

    public int IndexOf(ITrackQueueItem item) => redis.GetQueue(ID).IndexOf((EnrichedTrack)item);

    public int IndexOf(Func<ITrackQueueItem, bool> predicate)
    {
        var result = redis.GetQueue(ID);

        for (var index = 0; index < result.Count; index++)
        {
            if (predicate(result[index]))
            {
                return index;
            }
        }

        return -1;
    }

    public async ValueTask<bool> RemoveAtAsync(int index, CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await redis.GetQueueAsync(ID);
        if (index < 0 || index >= result.Count) return false;

        result.RemoveAt(index);
        await redis.SetQueueAsync(ID, result);
        return true;
    }

    public async ValueTask<bool> RemoveAsync(ITrackQueueItem item,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await redis.GetQueueAsync(ID);
        var removed = result.Remove((EnrichedTrack)item);
        await redis.SetQueueAsync(ID, result);
        return removed;
    }

    public async ValueTask<int> RemoveAllAsync(Predicate<ITrackQueueItem> predicate,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await redis.GetQueueAsync(ID);
        var previousCount = result.Count;
        result.RemoveAll(predicate);
        await redis.SetQueueAsync(ID, result);
        return previousCount - result.Count;
    }

    public async ValueTask RemoveRangeAsync(int index, int count,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await redis.GetQueueAsync(ID);
        result.RemoveRange(index, count);
        await redis.SetQueueAsync(ID, result);
    }

    public async ValueTask<int> DistinctAsync(IEqualityComparer<ITrackQueueItem>? equalityComparer = null,
        CancellationToken cancellationToken = new CancellationToken())
        => throw new NotImplementedException();

    public async ValueTask<int> AddAsync(ITrackQueueItem item,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await redis.GetQueueAsync(ID);
        result.Add((EnrichedTrack)item);
        await redis.SetQueueAsync(ID, result);
        return result.Count;
    }

    public async ValueTask<int> AddRangeAsync(IReadOnlyList<ITrackQueueItem> items,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await redis.GetQueueAsync(ID);
        result.AddRange((List<EnrichedTrack>)items);
        await redis.SetQueueAsync(ID, result);
        return result.Count;
    }

    public async ValueTask<int> ClearAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        await redis.ClearQueueAsync(ID);
        return 0;
    }

    public bool IsEmpty => Count is 0;

    public async ValueTask InsertAsync(int index, ITrackQueueItem item,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await redis.GetQueueAsync(ID);
        result.Insert(index, (EnrichedTrack)item);
        await redis.SetQueueAsync(ID, result);
    }

    public async ValueTask InsertRangeAsync(int index, IEnumerable<ITrackQueueItem> items,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await redis.GetQueueAsync(ID);
        result.InsertRange(index, (List<EnrichedTrack>)items);
        await redis.SetQueueAsync(ID, result);
    }

    public async ValueTask ShuffleAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await redis.GetQueueAsync(ID);
        for (var index = 0; index < result.Count; index++)
        {
            var targetIndex = index + Random.Shared.Next(result.Count - index);
            (result[index], result[targetIndex]) = (result[targetIndex], result[index]);
        }

        await redis.SetQueueAsync(ID, result);
    }

    public ITrackQueueItem? Peek()
    {
        var result = redis.GetQueue(ID);
        return result.FirstOrDefault();
    }

    public bool TryPeek(out ITrackQueueItem? queueItem)
    {
        queueItem = Peek();
        return queueItem is not null;
    }

    public async ValueTask<ITrackQueueItem?> TryDequeueAsync(TrackDequeueMode dequeueMode = TrackDequeueMode.Normal,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await redis.GetQueueAsync(ID);
        if (!result.Any()) return null;

        var index = dequeueMode is TrackDequeueMode.Shuffle
            ? Random.Shared.Next(0, result.Count)
            : 0;

        var track = result[index];
        result.RemoveAt(index);
        await redis.SetQueueAsync(ID, result);

        return track;
    }

    public ITrackHistory? History => null;
    public bool HasHistory => false;
}