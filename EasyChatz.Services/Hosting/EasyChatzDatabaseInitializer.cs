using Microsoft.Extensions.Hosting;

namespace EasyChatz.Services.Hosting;

/// <summary>Applies EasyChatz migrations at startup and makes sure the Announcements room exists.</summary>
internal sealed class EasyChatzDatabaseInitializer(
    ILogger<EasyChatzDatabaseInitializer> logger,
    IOptions<EasyChatzOptions> options,
    IEasyChatzDbContextFactory contextFactory,
    IRepository<ChatRoom> rooms) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.Value.AutoMigrate)
        {
            try
            {
                using var context = contextFactory.GetContext();
                await context.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("EasyChatz database is up to date ({Provider})", contextFactory.ProviderName);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Logged rather than thrown so a database outage doesn't take the host's own pages down with it.
                logger.LogError(ex, "Could not migrate the EasyChatz database ({Provider})", contextFactory.ProviderName);
                return;
            }
        }

        try
        {
            string name = options.Value.AnnouncementsRoomName;
            bool exists = await rooms.ExistsAsync(
                r => r.Name == name,
                ContextOptions.ForCancellationToken(cancellationToken));

            if (!exists)
            {
                await rooms.InsertAsync(new ChatRoom
                {
                    Name = name,
                    AdminUserId = Constants.SystemUserId,
                    CreatedAtUtc = DateTime.UtcNow,
                }, ContextOptions.ForCancellationToken(cancellationToken));

                logger.LogInformation("Created the {Room} room", name);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Could not seed the EasyChatz Announcements room");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
