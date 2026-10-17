using EasyChatz.Data;
using EasyChatz.Data.Entities;
using EasyChatz.Data.Sqlite;
using EasyChatz.Models;
using EasyChatz.Services;
using Extenso.Data.Entity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EasyChatz.Tests;

public sealed class TestCurrentUser : IChatCurrentUser
{
    public ChatUser? User { get; set; }

    public Task<ChatUser?> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(User);
}

public sealed class TestDirectory : IChatUserDirectory
{
    public static readonly ChatUser Alex = new("alex", "Alex Morgan", true, true);
    public static readonly ChatUser Blair = new("blair", "Blair Chen", false, true);

    public Task<ChatUser?> FindAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(userId switch
        {
            "alex" => Alex,
            "blair" => Blair,
            _ => null,
        });

    public Task<IReadOnlyDictionary<string, ChatUser>> FindManyAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        var found = new Dictionary<string, ChatUser>(StringComparer.Ordinal);
        foreach (string id in userIds.Distinct(StringComparer.Ordinal))
        {
            var user = id switch
            {
                "alex" => Alex,
                "blair" => Blair,
                _ => null,
            };

            if (user is not null)
            {
                found[user.Id] = user;
            }
        }

        return Task.FromResult<IReadOnlyDictionary<string, ChatUser>>(found);
    }
}

public sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection keepAlive;
    private readonly ServiceProvider provider;
    private readonly AsyncServiceScope scope;

    private TestDatabase(SqliteConnection keepAlive, ServiceProvider provider, TestCurrentUser currentUser)
    {
        this.keepAlive = keepAlive;
        this.provider = provider;
        scope = provider.CreateAsyncScope();
        CurrentUser = currentUser;
    }

    public TestCurrentUser CurrentUser { get; }

    public IChatService Chat => scope.ServiceProvider.GetRequiredService<IChatService>();

    public IChatNotificationService Notifications => scope.ServiceProvider.GetRequiredService<IChatNotificationService>();

    public IChatDeliveryQueue Delivery => scope.ServiceProvider.GetRequiredService<IChatDeliveryQueue>();

    public static async Task<TestDatabase> CreateAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        string connectionString = $"Data Source=ec-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var keepAlive = new SqliteConnection(connectionString);
        await keepAlive.OpenAsync(cancellationToken);

        var currentUser = new TestCurrentUser { User = TestDirectory.Alex };
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddSingleton<IChatCurrentUser>(currentUser);
        services.AddSingleton<IChatUserDirectory, TestDirectory>();
        services.AddEasyChatzServices().AddEasyChatzSqlite(connectionString);

        var provider = services.BuildServiceProvider(validateScopes: true);
        await using (var context = provider.GetRequiredService<IEasyChatzDbContextFactory>().GetContext())
        {
            await context.Database.MigrateAsync(cancellationToken);
        }

        var rooms = provider.GetRequiredService<IRepository<ChatRoom>>();
        await rooms.InsertAsync(new ChatRoom
        {
            Name = EasyChatz.Constants.AnnouncementsRoomName,
            AdminUserId = EasyChatz.Constants.SystemUserId,
            CreatedAtUtc = DateTime.UtcNow,
        });

        return new TestDatabase(keepAlive, provider, currentUser);
    }

    public async ValueTask DisposeAsync()
    {
        await scope.DisposeAsync();
        await provider.DisposeAsync();
        await keepAlive.DisposeAsync();
    }
}
