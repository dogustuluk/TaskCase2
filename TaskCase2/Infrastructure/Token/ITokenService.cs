namespace TaskCase2.Infrastructure.Token;

public interface ITokenService
{
    ValueTask<string> GetAccessTokenAsync(bool forceRefresh = false, CancellationToken ct = default);

}
