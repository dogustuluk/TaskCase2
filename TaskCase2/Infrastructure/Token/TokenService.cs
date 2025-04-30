using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.RateLimit;

namespace TaskCase2.Infrastructure.Token;


public sealed class TokenService : ITokenService
{
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _http;
    private readonly AsyncRateLimitPolicy _rate;
    private readonly ILogger<TokenService> _log;

    // *** TEST / PROD ayarları *******************************
#if DEBUG          //  dotnet run  →  Debug
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(30);
    private const int WindowQuota = 5;                        // 5 istek
    private const int TokenTtlSec = 5;                        // cache 5s
#else               //  dotnet run -c Release
    private static readonly TimeSpan Window        = TimeSpan.FromHours(1);
    private const           int      WindowQuota   = 5;
    private const           int      TokenTtlSec   = 3540; // ≃ 59 dk
#endif
    // *******************************************************

    private const string CacheKey = "access_token";

    public TokenService(IMemoryCache cache,
                        IHttpClientFactory http,
                        ILogger<TokenService> log)
    {
        _cache = cache;
        _http = http;
        _log = log;
        _rate = Policy.RateLimitAsync(WindowQuota, Window);
    }

    private sealed record TokenDto(string token_type,
                                   int expires_in,
                                   string access_token);

    public async ValueTask<string> GetAccessTokenAsync(
        bool forceRefresh = false,
        CancellationToken ct = default)
    {
        // 1) Cache kontrolü
        if (!forceRefresh && _cache.TryGetValue(CacheKey, out string cached))
        {
            _log.LogTrace("[Cache HIT] {Tok}", cached[..8]);
            return cached;
        }

        // 2) Polly rate-limit
        try
        {
            return await _rate.ExecuteAsync(async innerCt =>
            {
                _log.LogInformation("Polly /token isteği gönderiliyor…");

                var res = await _http.CreateClient("auth")
                                     .PostAsync("/token", null, innerCt);
                res.EnsureSuccessStatusCode();

                var dto = await res.Content.ReadFromJsonAsync<TokenDto>(
                              cancellationToken: innerCt)
                          ?? throw new Exception("Token çözümlenemedi");

                var ttl = TimeSpan.FromSeconds(TokenTtlSec);
                _cache.Set(CacheKey, dto.access_token, ttl);

                return dto.access_token;
            }, ct);
        }
        catch (RateLimitRejectedException)
        {
            _log.LogWarning("‼️ Token limiti aşıldı (>{Quota} istek/{Win})", WindowQuota, Window);
            throw;
        }
    }
}
