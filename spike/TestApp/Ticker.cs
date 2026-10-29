using System.Threading.Channels;

namespace TestApp;

// A hosted service that runs work handed to it on its own loop, outside any circuit.
public class Ticker : BackgroundService
{
    private readonly Channel<(Func<Task> Work, TaskCompletionSource Done)> _queue =
        Channel.CreateUnbounded<(Func<Task>, TaskCompletionSource)>();

    public Task RunOnServiceAsync(Func<Task> work)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Writer.TryWrite((work, done));
        return done.Task;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var (work, done) in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await work();
            done.SetResult();
        }
    }
}
