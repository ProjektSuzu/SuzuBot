namespace SuzuBot.Events;
internal class Observer<T> : IObserver<T>
{
    private readonly Func<Task>? _completed;
    private readonly Func<Exception, Task>? _error;
    private readonly Func<T, Task>? _next;

    private Observer(Func<T, Task>? next, Func<Exception, Task>? error, Func<Task>? completed)
    {
        _next = next;
        _error = error;
        _completed = completed;
    }

    public static IObserver<T> Create(Func<T, Task>? next, Func<Exception, Task>? error = null, Func<Task>? completed = null)
    {
        return new Observer<T>(next, error, completed);
    }

    public async void OnCompleted()
    {
        if (_completed is not null)
            await _completed.Invoke();
    }

    public async void OnError(Exception error)
    {
        if (error is not null)
            await _error.Invoke(error);
    }

    public async void OnNext(T value)
    {
        if (_next is not null)
            await _next.Invoke(value);
    }
}
