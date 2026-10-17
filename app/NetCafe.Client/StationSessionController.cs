// Trách nhiệm: duy nhất một nơi quản lý heartbeat 30 giây và trạng thái session station.
// Liên kết: DbRepository.StationHeartbeatAsync và SessionView.

using NetCafe.Data;

namespace NetCafe.Client;

public sealed class StationSessionController : IAsyncDisposable
{
    private readonly DbRepository _repository;
    private readonly StationConfig _config;
    private CancellationTokenSource? _heartbeatCancellation;
    private Task? _heartbeatTask;

    public StationLoginResult Session { get; private set; } = null!;
    public event Action<StationHeartbeatResult>? HeartbeatReceived;
    public event Action<Exception>? HeartbeatFailed;

    public StationSessionController(DbRepository repository, StationConfig config)
    {
        _repository = repository;
        _config = config;
    }

    public void Start(StationLoginResult session)
    {
        Stop();
        Session = session;
        _heartbeatCancellation = new CancellationTokenSource();
        _heartbeatTask = Task.Run(() => HeartbeatLoopAsync(_heartbeatCancellation.Token));
    }

    public void Stop()
    {
        _heartbeatCancellation?.Cancel();
        _heartbeatCancellation?.Dispose();
        _heartbeatCancellation = null;
    }

    public async ValueTask DisposeAsync()
    {
        var heartbeatTask = _heartbeatTask;
        Stop();
        _heartbeatTask = null;
        if (heartbeatTask is not null)
        {
            try { await heartbeatTask; } catch (OperationCanceledException) { }
        }
    }

    private async Task HeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, _config.HeartbeatSeconds)));
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                var result = await _repository.StationHeartbeatAsync(
                    Session.SessionId, Session.CustomerId, Session.ComputerId, cancellationToken);
                HeartbeatReceived?.Invoke(result);
                if (result.SessionStatus == "finished")
                {
                    Stop();
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                HeartbeatFailed?.Invoke(exception);
            }
        }
    }
}
