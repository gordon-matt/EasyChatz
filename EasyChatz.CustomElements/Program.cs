using EasyChatz;
using EasyChatz.CustomElements;
using EasyChatz.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.RegisterCustomElement<Chat>("ec-chat");

string apiBaseUrl = builder.Configuration["EasyChatz:ApiBaseUrl"]
    ?? throw new InvalidOperationException("Set EasyChatz:ApiBaseUrl to the EasyChatz API, for example https://localhost:7411/api/easychatz.");

string hubUrl = builder.Configuration["EasyChatz:HubUrl"]
    ?? throw new InvalidOperationException("Set EasyChatz:HubUrl to the EasyChatz hub, for example https://localhost:7411/chathub.");

bool includeCredentials = builder.Configuration.GetValue<bool>("EasyChatz:IncludeCredentials");

builder.Services.AddTransient<IncludeCredentialsHandler>();
builder.Services.AddEasyChatzHttpClient(apiBaseUrl, hubUrl, includeCredentials, client =>
{
    if (includeCredentials)
    {
        client.AddHttpMessageHandler<IncludeCredentialsHandler>();
    }
});

await builder.Build().RunAsync();
