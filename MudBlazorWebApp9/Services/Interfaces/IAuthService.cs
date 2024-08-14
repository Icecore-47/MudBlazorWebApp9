using MudBlazorWebApp9.Models;

public interface IAuthService
{
    Task<TokenResponse> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<string> GetStoredTokenAsync(CancellationToken cancellationToken = default);
    Task<string> GetStoredTokenWithAutoRefreshAsync(CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
}