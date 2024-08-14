using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace MudBlazorWebApp9.Services
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public CustomAuthenticationStateProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // Resolve IJSRuntime when needed
            var jsRuntime = _serviceProvider.GetRequiredService<IJSRuntime>();

            var token = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            ClaimsIdentity identity;

            if (!string.IsNullOrEmpty(token))
            {
                // If token exists, validate and create ClaimsPrincipal from the token
                identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
            }
            else
            {
                // If no token, create an empty identity
                identity = new ClaimsIdentity();
            }

            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }

        public async Task LogoutAsync()
        {
            var jsRuntime = _serviceProvider.GetRequiredService<IJSRuntime>();
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");

            var identity = new ClaimsIdentity();
            var user = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
            return keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()));
        }

        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}
