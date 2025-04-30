using TaskCase2.Features.Orders;

namespace TokenSample.Features.Orders;

public sealed class OrderPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderPollingService> _log;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMinutes(5));

    public OrderPollingService(IServiceScopeFactory scopeFactory,
                               ILogger<OrderPollingService> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        await FetchAsync(stop);

        while (await _timer.WaitForNextTickAsync(stop))
            await FetchAsync(stop);
    }

    private async Task FetchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetOrders.Handler>();

        try { await handler.HandleAsync(new(), ct); }
        catch (Exception ex)
        { _log.LogError(ex, "Polling hata"); }
    }
}

