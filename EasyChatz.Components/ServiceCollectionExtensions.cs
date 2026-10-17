using EasyChatz.Components.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyChatz;

public static class ComponentServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Points the chat UI at an EasyChatz API on another origin. Used by the WebAssembly custom element.
        /// Blazor Server and MVC should call <c>AddEasyChatz()</c> instead and use the services in-process.
        /// </summary>
        public IServiceCollection AddEasyChatzHttpClient(
            string apiBaseUrl,
            string hubUrl,
            bool includeCredentials = false,
            Action<IHttpClientBuilder>? configureClient = null)
        {
            string api = apiBaseUrl.TrimEnd('/') + "/";
            var builder = services.AddHttpClient<EasyChatzApiClient>(client => client.BaseAddress = new Uri(api, UriKind.Absolute));
            configureClient?.Invoke(builder);

            services.TryAddScoped<IChatService, HttpChatService>();
            services.TryAddScoped<IChatNotificationService, HttpChatNotificationService>();
            services.TryAddSingleton(new EasyChatzApiAddress(api, hubUrl, includeCredentials));
            return services;
        }
    }
}
