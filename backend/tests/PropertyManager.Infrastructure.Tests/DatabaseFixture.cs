using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PropertyManager.Infrastructure.Tests;

/// <summary>
/// Test fixture that provides a PostgreSQL container for integration tests.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public DatabaseFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("propertymanager_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public AppDbContext CreateDbContext(ICurrentUser? currentUser = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return currentUser != null
            ? new AppDbContext(options, currentUser)
            : new AppDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply migrations
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

/// <summary>
/// Test implementation of ICurrentUser for multi-tenancy testing.
/// </summary>
public class TestCurrentUser : ICurrentUser
{
    public Guid UserId { get; set; }
    public Guid AccountId { get; set; }
    public string Role { get; set; } = "Owner";
    public bool IsAuthenticated { get; set; } = true;
}

/// <summary>
/// Collection definition for sharing database fixture across tests.
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
