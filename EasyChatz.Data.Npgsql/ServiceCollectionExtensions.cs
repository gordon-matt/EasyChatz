using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyChatz.Data.Npgsql;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Stores EasyChatz rooms and messages in PostgreSQL.</summary>
        public IServiceCollection AddEasyChatzNpgsql(string connectionString)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            services.RemoveAll<IEasyChatzDbContextFactory>();
            services.AddSingleton<IEasyChatzDbContextFactory>(new EasyChatzDbContextFactory(connectionString));

            return services;
        }
    }
}
