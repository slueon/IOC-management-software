using System.Diagnostics;
using IOC.Core.Contracts.Adapters;

namespace IOC.Infrastructure.Adapters;

public abstract class BaseDeviceAdapter<TDevice, TTelemetry> : IDeviceAdapter<TDevice, TTelemetry>
{
    public abstract AdapterProtocol Protocol { get; }
    public AdapterRetryPolicy RetryPolicy { get; protected set; }

    protected BaseDeviceAdapter(AdapterRetryPolicy? retryPolicy = null)
    {
        RetryPolicy = retryPolicy ?? AdapterRetryPolicy.Default;
    }

    public async Task<AdapterPollResult<TTelemetry>> PollAsync(TDevice device, CancellationToken cancellationToken = default)
    {
        int attempts = 0;
        var stopwatch = Stopwatch.StartNew();
        Exception? lastException = null;

        while (attempts < Math.Max(1, RetryPolicy.MaxRetries))
        {
            attempts++;
            cancellationToken.ThrowIfCancellationRequested();

            using var timeoutCts = new CancellationTokenSource(RetryPolicy.Timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                var data = await ExecutePollAsync(device, linkedCts.Token);
                stopwatch.Stop();
                return AdapterPollResult<TTelemetry>.Success(data, (int)stopwatch.ElapsedMilliseconds, DeviceHealthState.Online, attempts);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = new TimeoutException($"Quá thời gian kết nối ({RetryPolicy.Timeout.TotalMilliseconds}ms) tại lần thử thứ {attempts}");
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            if (attempts < RetryPolicy.MaxRetries)
            {
                try
                {
                    await Task.Delay(RetryPolicy.BackoffInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        stopwatch.Stop();
        var isTimeout = lastException is TimeoutException;
        var health = isTimeout ? DeviceHealthState.Timeout : DeviceHealthState.Offline;
        var errorMsg = lastException?.Message ?? "Lỗi kết nối không xác định";

        return AdapterPollResult<TTelemetry>.Failure(errorMsg, health, attempts, (int)stopwatch.ElapsedMilliseconds);
    }

    protected abstract Task<TTelemetry> ExecutePollAsync(TDevice device, CancellationToken cancellationToken);
}
