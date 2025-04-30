using TaskCase2.Infrastructure.Token;

namespace TaskCase2.Features.Orders;

public static class GetOrders
{
    public sealed record Query;
    public sealed record OrderDto(int Id, decimal Total);

    public sealed class Handler
    {
        private readonly ITokenService _tokenSvc;
        private readonly IHttpClientFactory _factory;
        private readonly ILogger<Handler> _log;

        public Handler(ITokenService tokenSvc,
                       IHttpClientFactory factory,
                       ILogger<Handler> log)
        {
            _tokenSvc = tokenSvc;
            _factory = factory;
            _log = log;
        }

        public async Task<IReadOnlyList<OrderDto>> HandleAsync(
            Query _,
            CancellationToken ct = default)
        {
            var client = _factory.CreateClient("orders");
            client.DefaultRequestHeaders.Authorization = new("Bearer", await _tokenSvc.GetAccessTokenAsync(ct: ct));

            var rsp = await client.GetAsync("/remote/orders", ct);
            rsp.EnsureSuccessStatusCode();

            var data = await rsp.Content.ReadFromJsonAsync<OrderDto[]>(cancellationToken: ct)
                       ?? Array.Empty<OrderDto>();

            _log.LogInformation("{Count} sipariş.", data.Length);
            return data;
        }
    }
}

