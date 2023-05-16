using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using SuzuBot.EventArgs;

namespace SuzuBot.Events;
internal class EventBus : IObservable<SuzuEventArgs>, IHostedService
{
    private readonly Channel<SuzuEventArgs> _channel = Channel.CreateUnbounded<SuzuEventArgs>();
    private readonly List<IObserver<SuzuEventArgs>> _observers = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly Thread _thread;

    public EventBus()
    {
        _thread = new Thread(async () =>
        {
            while (!_cancellationTokenSource.IsCancellationRequested && await _channel.Reader.WaitToReadAsync())
            {
                var eventArgs = await _channel.Reader.ReadAsync(_cancellationTokenSource.Token);
                await Parallel.ForEachAsync(_observers, async (observer, token) =>
                {
                    await Task.Yield();
                    if (token.IsCancellationRequested) return;
                    observer.OnNext(eventArgs);
                }).ConfigureAwait(false);
            }
        });
    }

    public ValueTask PublishEvent(SuzuEventArgs eventArgs)
    {
        return _channel.Writer.WriteAsync(eventArgs);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _thread.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource.Cancel();
        _channel.Writer.Complete();
        foreach (var observer in _observers)
            observer.OnCompleted();
        _observers.Clear();
        return Task.CompletedTask;
    }

    public IDisposable Subscribe(IObserver<SuzuEventArgs> observer)
    {
        _observers.Add(observer);
        return Disposable.Create(() => _observers.Remove(observer));
    }
}
