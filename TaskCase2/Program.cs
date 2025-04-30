using Polly.RateLimit;
using TaskCase2.Features.Orders;
using TaskCase2.Infrastructure.Token;
using TokenSample.Features.Orders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddMemoryCache();

var baseUri = new Uri("https://localhost:7104");
builder.Services.AddHttpClient("auth", c => c.BaseAddress = baseUri);
builder.Services.AddHttpClient("orders", c => c.BaseAddress = baseUri);




builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddTransient<GetOrders.Handler>();
builder.Services.AddHostedService<OrderPollingService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapPost("/token", () =>
{
    // 1 saat geçerli, rastgele bir token
    return Results.Ok(new
    {
        token_type = "Bearer",
        access_token = Guid.NewGuid().ToString("N"),
        expires_in = 3600
    });
});

var fakeOrders = new[]
{
    new GetOrders.OrderDto(1, 250m),
    new GetOrders.OrderDto(2, 180m),
    new GetOrders.OrderDto(3,  99m)
};
app.MapGet("/remote/orders", () => Results.Ok(fakeOrders));


// 2) Sipariş endpoint
app.MapGet("/orders",
    async (GetOrders.Handler handler, CancellationToken ct) =>
    {
        var list = await handler.HandleAsync(new GetOrders.Query(), ct);
        return Results.Ok(list);
    });

app.MapGet("/test-token", async (ITokenService svc) =>
{
    var outp = new List<string>();
    //for (int i = 1; i <= 17; i++)
    //{
    try
    {
        var t = await svc.GetAccessTokenAsync(forceRefresh: true);
        // outp.Add($"Çağrı {i}: {t[..8]}");
        outp.Add($"Çağrı Yapıldı");
    }
    catch (RateLimitRejectedException)
    {
        outp.Add($"‼️ RateLimitRejected");
        //.Add($"Çağrı {i}: ‼️ RateLimitRejected");
    }
    // }
    return outp;
});





app.Run();
