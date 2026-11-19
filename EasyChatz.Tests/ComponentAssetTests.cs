using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace EasyChatz.Tests;

public sealed class ComponentAssetTests
{
    [Fact]
    public async Task Assemblies_serve_the_stylesheet_and_scripts_without_a_wwwroot_copy()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");
        var app = builder.Build();
        app.MapEasyChatzAssets();
        await app.StartAsync(cancellationToken);
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        try
        {
            var script = await client.GetAsync("/_content/EasyChatz.Components/easychatz.js", cancellationToken);
            Assert.Equal(HttpStatusCode.OK, script.StatusCode);
            Assert.Equal("text/javascript", script.Content.Headers.ContentType?.MediaType);
            string body = await script.Content.ReadAsStringAsync(cancellationToken);
            Assert.Contains("signalr.min.js", body, StringComparison.Ordinal);

            var signalr = await client.GetAsync("/_content/EasyChatz.Components/signalr.min.js", cancellationToken);
            Assert.Equal(HttpStatusCode.OK, signalr.StatusCode);

            var css = await client.GetAsync("/_content/EasyChatz.Components/easychatz.css", cancellationToken);
            Assert.Equal(HttpStatusCode.OK, css.StatusCode);
            Assert.Equal("text/css", css.Content.Headers.ContentType?.MediaType);

            var etag = script.Headers.ETag;
            Assert.NotNull(etag);
            var again = new HttpRequestMessage(HttpMethod.Get, "/_content/EasyChatz.Components/easychatz.js");
            again.Headers.IfNoneMatch.Add(etag);
            var revalidated = await client.SendAsync(again, cancellationToken);
            Assert.Equal(HttpStatusCode.NotModified, revalidated.StatusCode);

            var escaped = await client.GetAsync("/_content/EasyChatz.Components/..%2Fsecret.txt", cancellationToken);
            Assert.Equal(HttpStatusCode.NotFound, escaped.StatusCode);
        }
        finally
        {
            await app.StopAsync(cancellationToken);
            await app.DisposeAsync();
        }
    }
}
