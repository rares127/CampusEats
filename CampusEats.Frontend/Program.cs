using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using CampusEats.Frontend;
using CampusEats.Frontend.Services;
using CampusEats.Frontend.State;
using CampusEats.Frontend.Features.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ✅ CITEȘTE DIN appsettings.json
var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000";

builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<CampusEats.Frontend.Services.OrderService>();
builder.Services.AddScoped<LoyaltyService>();
builder.Services.AddAuthorizationCore();

builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services.AddTransient<DebugAuthHandler>();

// Client implicit "authorized" (orice @inject HttpClient va avea Bearer)
builder.Services.AddHttpClient("authorized", client =>
    {
        client.BaseAddress = new Uri(apiBase);
    })
    .AddHttpMessageHandler<AuthHeaderHandler>()
    .AddHttpMessageHandler<DebugAuthHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("authorized"));

// ApiClient tipizat – tot cu Bearer
builder.Services.AddHttpClient<ApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiBase);
    })
    .AddHttpMessageHandler<AuthHeaderHandler>()
    .AddHttpMessageHandler<DebugAuthHandler>();

builder.Services.AddScoped<CartService>();

var host = builder.Build();

// Rehidratează token-ul înainte de primul request
var auth = host.Services.GetRequiredService<AuthService>();
try
{
    await auth.InitializeAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"[CRITIC] Eroare la inițializarea Auth: {ex.Message}");
}

await host.RunAsync();