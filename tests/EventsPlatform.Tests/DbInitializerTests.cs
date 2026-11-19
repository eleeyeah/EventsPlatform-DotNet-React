using Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Xunit;

namespace EventsPlatform.Tests;

/// <summary>The seed data: it fills an empty database once, and running it again adds nothing.</summary>
public class DbInitializerTests
{
    private static AppDbContext NewContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task Seeding_an_empty_database_adds_ten_events()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        await using var context = NewContext(connection);

        await DbInitializer.SeedData(context);

        Assert.Equal(10, await context.Events.CountAsync());
    }

    [Fact]
    public async Task Seeding_twice_does_not_duplicate_events()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        await using var context = NewContext(connection);

        await DbInitializer.SeedData(context);
        await DbInitializer.SeedData(context);

        Assert.Equal(10, await context.Events.CountAsync());
    }

    [Fact]
    public async Task The_seeded_events_are_split_between_past_and_future()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        await using var context = NewContext(connection);
        await DbInitializer.SeedData(context);

        var now = DateTime.UtcNow;
        var past = await context.Events.CountAsync(e => e.Date < now);
        var future = await context.Events.CountAsync(e => e.Date > now);

        Assert.Equal(2, past);
        Assert.Equal(8, future);
    }
}
