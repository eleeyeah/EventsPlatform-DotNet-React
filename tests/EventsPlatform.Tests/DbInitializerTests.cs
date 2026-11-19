using Domain;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Xunit;

namespace EventsPlatform.Tests;

/// <summary>The seed data: it fills an empty database once, and running it again adds nothing.</summary>
public class DbInitializerTests
{
    private static AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task Seeding_an_empty_database_adds_ten_events()
    {
        await using var context = NewContext();

        await DbInitializer.SeedData(context);

        Assert.Equal(10, await context.Events.CountAsync());
    }

    [Fact]
    public async Task Seeding_twice_does_not_duplicate_events()
    {
        await using var context = NewContext();

        await DbInitializer.SeedData(context);
        await DbInitializer.SeedData(context);

        Assert.Equal(10, await context.Events.CountAsync());
    }

    [Fact]
    public async Task The_seeded_events_are_split_between_past_and_future()
    {
        await using var context = NewContext();
        await DbInitializer.SeedData(context);

        var now = DateTime.UtcNow;
        var past = await context.Events.CountAsync(e => e.Date < now);
        var future = await context.Events.CountAsync(e => e.Date > now);

        Assert.Equal(2, past);
        Assert.Equal(8, future);
    }
}
