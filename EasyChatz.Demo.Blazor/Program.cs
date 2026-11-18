using EasyChatz;
using EasyChatz.Data.Sqlite;
using EasyChatz.Demo;
using EasyChatz.Demo.Blazor.Components;
using Humanizer;

const string EmbedCorsPolicy = "EasyChatzEmbeds";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddDemoUsers();
builder.Services
    .AddEasyChatz(options =>
    {
        options.SystemDisplayName = "EasyChatz Demo";

        // For demo purposes, let anyone chat with anyone.
        // If this is enabled, the host needs to provide a way for users to initiate conntact with each other
        options.RequireConnections = false;
    })
    .AddEasyChatzSqlite(builder.Configuration.GetConnectionString("EasyChatz"));

builder.Services.AddCors(options => options.AddPolicy(EmbedCorsPolicy, policy => policy
    .WithOrigins(builder.Configuration.GetSection("EasyChatz:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapDemoAuth(EmbedCorsPolicy);
app.MapStaticAssets();
app.MapEasyChatzApi().RequireAuthorization().RequireCors(EmbedCorsPolicy);
app.MapEasyChatzHub().RequireAuthorization().RequireCors(EmbedCorsPolicy);
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
