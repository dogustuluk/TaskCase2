using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.RateLimit;

namespace TaskCase2.Infrastructure.Token;


public sealed class TokenService : ITokenService
{
    private const string CacheKey = "access_token";

    // access_token süresi(sn) – 2 dakika güvenlik payı
    private static readonly TimeSpan TokenTtl = TimeSpan.FromMinutes(58);

    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _http;
    private readonly AsyncRateLimitPolicy _rate;
    private readonly ILogger<TokenService> _log;

    public TokenService(IMemoryCache cache,
                        IHttpClientFactory http,
                        ILogger<TokenService> log)
    {
        _cache = cache;
        _http = http;
        _log = log;

        // 30 saniyede en fazla 5 istek
        _rate = Policy.RateLimitAsync(5, TimeSpan.FromSeconds(30), maxBurst: 5); //test için 1 saat yerine 30 saniye ve burst verilerek tek seferde 5 isteği kabul edecek hale geldi
    }

    private sealed record TokenDto(string token_type,
                                   int expires_in,
                                   string access_token);

    public async ValueTask<string> GetAccessTokenAsync(
        bool forceRefresh = false,
        CancellationToken ct = default)
    {
        if (!forceRefresh && _cache.TryGetValue(CacheKey, out string token))
            return token;

        try
        {
            return await _rate.ExecuteAsync(async innerCt =>
            {
                var rsp = await _http.CreateClient("auth").PostAsync("/token", null, innerCt);
                rsp.EnsureSuccessStatusCode();

                var dto = await rsp.Content.ReadFromJsonAsync<TokenDto>(cancellationToken: innerCt)
                          ?? throw new Exception("Token çözümlenemedi");

                // token ömründen 2 dakika eksilterek saklarım
                _cache.Set(CacheKey, dto.access_token, TokenTtl);

                return dto.access_token;
            }, ct);
        }
        catch (RateLimitRejectedException)
        {
            _log.LogWarning("Son 1 saat içinde 5’ten fazla token isteği atıldı (RateLimitRejected)");
            throw;
        }
    }
}

