using EasyChatz;
using EasyChatz.Data.Sqlite;
using EasyChatz.Demo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddServerSideBlazor();
builder.Services.AddDemoUsers();
builder.Services
    .AddEasyChatz()
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
