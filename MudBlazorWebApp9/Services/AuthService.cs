using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazorWebApp9.Models;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private const string TokenStorageKey = "authToken";
    private const string RefreshTokenStorageKey = "refreshToken";

    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public AuthService(HttpClient httpClient, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    private string TokenEndpoint => _configuration["Keycloak:TokenEndpoint"];
    private string ClientId => _configuration["Keycloak:ClientId"];

    public async Task<TokenResponse> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            });

            var response = await _httpClient.PostAsync(TokenEndpoint, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(jsonResponse, _jsonOptions);

                // Store the access and refresh tokens securely
                return tokenResponse;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new AuthenticationException($"Authentication failed: {error}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred during authentication.");
            throw new AuthenticationException("A network error occurred while attempting to authenticate.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during authentication.");
            throw new AuthenticationException("An unexpected error occurred during authentication.", ex);
        }
    }

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        var response = await _httpClient.PostAsync(TokenEndpoint, content, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(jsonResponse, _jsonOptions);

            return tokenResponse;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new AuthenticationException($"Token refresh failed: {error}");
        }
    }

    public async Task<string> GetStoredTokenAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GetStoredTokenWithAutoRefreshAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GetStoredTokenAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken = default)
    {
        return await jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenStorageKey);
    }

    public async Task<string> GetStoredTokenWithAutoRefreshAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken = default)
    {
        var token = await GetStoredTokenAsync(jsRuntime, cancellationToken);
        if (IsTokenExpired(token))
        {
            var refreshToken = await jsRuntime.InvokeAsync<string>("localStorage.getItem", RefreshTokenStorageKey);
            var newTokenResponse = await RefreshTokenAsync(refreshToken, cancellationToken);
            return newTokenResponse.AccessToken;
        }
        return token;
    }

    public async Task LogoutAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken = default)
    {
        try
        {
            var refreshToken = await jsRuntime.InvokeAsync<string>("localStorage.getItem", RefreshTokenStorageKey);

            if (!string.IsNullOrEmpty(refreshToken))
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", ClientId),
                    new KeyValuePair<string, string>("refresh_token", refreshToken)
                });

                var logoutEndpoint = _configuration["Keycloak:LogoutEndpoint"];
                var response = await _httpClient.PostAsync(logoutEndpoint, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Server logout failed with status code {StatusCode}.", response.StatusCode);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while logging out from the server.");
        }

        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenStorageKey);
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", RefreshTokenStorageKey);
    }

    private bool IsTokenExpired(string token)
    {
        if (string.IsNullOrEmpty(token)) return true;

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        return jwtToken.ValidTo < DateTime.UtcNow.AddMinutes(-5);
    }

    private string EncryptToken(string token)
    {
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(token)); // Example, replace with proper encryption
    }

    private string DecryptToken(string encryptedToken)
    {
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encryptedToken)); // Example, replace with proper decryption
    }
}
