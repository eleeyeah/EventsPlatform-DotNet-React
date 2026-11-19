using API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Persistence;

namespace EventsPlatform.Tests;

/// <summary>
/// Runs the real API against an in-memory database instead of PostgreSQL. The schema is created explicitly
/// because the app's startup migration is written for PostgreSQL and its errors are only logged.
/// </summary>
public sealed class EventsPlatformFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // EF Core allows one provider per container. Remove the PostgreSQL registrations before adding the in-memory provider.
            var providerServices = services.Where(IsProviderRegistration).ToList();
            foreach (var descriptor in providerServices) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
        DbInitializer.SeedData(context).GetAwaiter().GetResult();
        return host;
    }

    private static bool IsProviderRegistration(ServiceDescriptor d)
    {
        static bool Provider(Type? t) =>
            t is not null && (t.Namespace?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true
                              || t.Namespace?.StartsWith("Npgsql", StringComparison.Ordinal) == true);
        return Provider(d.ServiceType) || Provider(d.ImplementationType) || d.ServiceType == typeof(DbContextOptions<AppDbContext>);
    }
}
