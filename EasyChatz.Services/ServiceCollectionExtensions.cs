using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyChatz.Services;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers EasyChatz services and Extenso repositories bound to <see cref="IEasyChatzDbContextFactory"/>.
        /// Add a database as well, for example <c>AddEasyChatzSqlite()</c>, and register
        /// <see cref="IChatUserDirectory"/> first so display names come from the host.
        /// </summary>
        public IServiceCollection AddEasyChatzServices(Action<EasyChatzOptions>? configure = null)
        {
            var optionsBuilder = services.AddOptions<EasyChatzOptions>().BindConfiguration(EasyChatzOptions.SectionName);
            if (configure is not null)
            {
                optionsBuilder.Configure(configure);
            }

            services.AddEasyChatzRepository<ChatRoom>();
            services.AddEasyChatzRepository<ChatMessage>();
            services.AddEasyChatzRepository<MessageNotification>();

            services.TryAddSingleton<IChatRealtime, NullChatRealtime>();
            services.TryAddScoped<IChatUserDirectory, FallbackChatUserDirectory>();
            services.TryAddScoped<IChatCurrentUser, UnauthenticatedChatUser>();
            services.TryAddScoped<IChatService, ChatService>();
            services.TryAddScoped<IChatNotificationService, ChatNotificationService>();

            services.AddHostedService<Hosting.EasyChatzDatabaseInitializer>();
            return services;
        }

        private void AddEasyChatzRepository<TEntity>()
            where TEntity : class, IEntity
        {
            // Replace, so a host that calls AddEntityFrameworkRepository() for its own context
            // does not leave EasyChatz reading the wrong database.
            services.Replace(ServiceDescriptor.Singleton<IRepository<TEntity>>(
                sp => new EntityFrameworkRepository<TEntity>(sp.GetRequiredService<IEasyChatzDbContextFactory>())));
        }
    }
}
