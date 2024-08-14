using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using MudBlazor.Services;
using MudBlazorWebApp9.Components;
using MudBlazorWebApp9.Services;

var builder = WebApplication.CreateBuilder(args);
var keycloakSettings = builder.Configuration.GetSection("Keycloak");
builder.Services.AddHttpClient(); // Register HttpClient for DI
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSignalR();
builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();


builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://auth.mysticit.co.uk/realms/myrealm";  // Your Keycloak realm base URL
        options.Audience = keycloakSettings["ClientId"]; // The client ID for your Keycloak client
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://auth.mysticit.co.uk/realms/myrealm",
            ValidateAudience = true,
            ValidAudience = keycloakSettings["ClientId"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true // Ensure the token's signing key is valid
        };
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();