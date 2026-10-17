using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyChatz.Data.Sqlite;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Stores EasyChatz rooms and messages in SQLite.
        /// </summary>
        /// <param name="connectionString">
        /// A SQLite connection string. When omitted, data goes to <c>EasyChatz.db</c> under the
        /// user's local application data folder, which survives redeployments of the app's own folder.
        /// </param>
        public IServiceCollection AddEasyChatzSqlite(string? connectionString = null)
        {
            string resolved = string.IsNullOrWhiteSpace(connectionString)
                ? $"Data Source={DefaultDatabasePath()}"
                : connectionString;

            services.RemoveAll<IEasyChatzDbContextFactory>();
            services.AddSingleton<IEasyChatzDbContextFactory>(new EasyChatzDbContextFactory(resolved));

            return services;
        }
    }

    private static string DefaultDatabasePath()
    {
        string appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "EasyChatz";

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName,
            "EasyChatz.db");
    }
}
