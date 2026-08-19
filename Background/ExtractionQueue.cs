using System.Collections.Concurrent;

namespace TechnicalChallenge.API.Background;

public class ExtractionQueue
{
    private readonly ConcurrentQueue<int> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);

    public void Enqueue(int extractionId)
    {
        _queue.Enqueue(extractionId);
        _signal.Release();
    }

    public async Task<int> DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);
        _queue.TryDequeue(out var extractionId);
        return extractionId;
    }
}
