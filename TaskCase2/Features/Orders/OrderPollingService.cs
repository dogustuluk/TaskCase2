using TaskCase2.Features.Orders;

namespace TokenSample.Features.Orders;

public sealed class OrderPollingService : BackgroundService
{
    private readonly GetOrders.Handler _handler;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMinutes(5));

    public OrderPollingService(GetOrders.Handler handler) => _handler = handler;

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        await Fetch(stop);
        while (await _timer.WaitForNextTickAsync(stop))
            await Fetch(stop);
    }

    private async Task Fetch(CancellationToken ct)
    {
        try { await _handler.HandleAsync(new(), ct); }
        catch (Exception ex) { Console.WriteLine($"Polling hata: {ex.Message}"); }
    }
}
