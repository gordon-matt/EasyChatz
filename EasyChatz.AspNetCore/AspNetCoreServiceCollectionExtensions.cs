using Microsoft.Extensions.Options;

namespace EasyChatz;

public static class AspNetCoreServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers EasyChatz, including the SignalR backplane used to push messages.
        /// Choose a database as well, for example <c>AddEasyChatzSqlite()</c>. Register
        /// <see cref="IChatUserDirectory"/> before this so names come from the host's accounts.
        /// </summary>
        public IServiceCollection AddEasyChatz(Action<EasyChatzOptions>? configure = null)
        {
            services.AddEasyChatzServices(configure);
            services.AddProblemDetails();
            services.AddSignalR();
            services.AddHttpContextAccessor();

            services.Replace(ServiceDescriptor.Scoped<IChatCurrentUser, ChatCurrentUser>());
            services.Replace(ServiceDescriptor.Singleton<IChatRealtime, SignalRChatRealtime>());

            services.TryAddSingleton(provider =>
            {
                var options = provider.GetRequiredService<IOptions<EasyChatzOptions>>().Value;
                return EasyChatzApiAddress.Relative(options.ApiPrefix, options.HubPath);
            });

            return services;
        }
    }
}
