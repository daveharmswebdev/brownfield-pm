using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PropertyManager.Api.Tests;

public class PropertyManagerWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Remove the IAppDbContext registration too
            var appDbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IAppDbContext));

            if (appDbContextDescriptor != null)
            {
                services.Remove(appDbContextDescriptor);
            }

            // Add test database
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_postgres.GetConnectionString());
            });

            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            // Replace email service with a singleton fake for testing
            var emailServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmailService));

            if (emailServiceDescriptor != null)
            {
                services.Remove(emailServiceDescriptor);
            }

            services.AddSingleton<FakeEmailService>();
            services.AddSingleton<IEmailService>(sp => sp.GetRequiredService<FakeEmailService>());
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Apply migrations
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.StopAsync();
        await base.DisposeAsync();
    }
}

public class FakeEmailService : IEmailService
{
    public List<(string Email, string Token)> SentVerificationEmails { get; } = [];
    public List<(string Email, string Token)> SentPasswordResetEmails { get; } = [];
    public List<(string Email, string Token)> SentInvitationEmails { get; } = [];

    public Task SendVerificationEmailAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        SentVerificationEmails.Add((email, token));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        SentPasswordResetEmails.Add((email, token));
        return Task.CompletedTask;
    }

    public Task SendInvitationEmailAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        SentInvitationEmails.Add((email, token));
        return Task.CompletedTask;
    }
}
