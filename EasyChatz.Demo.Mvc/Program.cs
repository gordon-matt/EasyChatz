using EasyChatz;
using EasyChatz.Data.Sqlite;
using EasyChatz.Demo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddServerSideBlazor();
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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapDemoAuth();
app.MapStaticAssets();
app.MapControllers();
app.MapBlazorHub();
app.MapEasyChatzApi().RequireAuthorization();
app.MapEasyChatzHub().RequireAuthorization();

app.Run();
