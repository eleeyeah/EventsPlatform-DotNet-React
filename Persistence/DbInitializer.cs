using System.Diagnostics;
using Domain;

namespace Persistence;

public static class DbInitializer
{
    public static async Task SeedData (AppDbContext context)
    {
        if (context.Events.Any()) return;
        var events = new List<Event>
        {
            new()
            {
                Title = "Past Event 1",
                Date = DateTime.UtcNow.AddMonths(-2),
                Description = "Event 2 months ago",
                Category = "drinks",
                City = "London",
                Venue =
                    "The Lamb and Flag, 33, Rose Street, Seven Dials, Covet Garden, London, Greater London, England, WC2E 9EB, United Kingdom",
                Latitude = 51.51171665,
                Longitude = -0.1256611057818921,
            },
            new()
            {
                Title = "Past Event 2",
                Date = DateTime.UtcNow.AddMonths(-1),
                Description = "Event 1 month ago",
                Category = "culture",
                City = "Paris",
                Venue =
                    "Louvre Museum, Rue Saint-Honor, Quartier du Palais Royal, 1st Arrondissement, Paris, Ile-de-France, Metropolitan France, 75001, France",
                Latitude = 48.8611473,
                Longitude = 2.33802768704666
            },
            new()
            {
                Title = "Future Event 1",
                Date = DateTime.UtcNow.AddMonths(1),
                Description = "Event 1 month in future",
                Category = "culture",
                City = "London",
                Venue = "Natural History Museum",
                Latitude = 51.496510900000004,
                Longitude = -0.17600190725447445
            },
            new()
            {
                Title = "Future Event 2",
                Date = DateTime.UtcNow.AddMonths(2),
                Description = "Event 2 months in future",
                Category = "music",
                City = "London",
                Venue = "The O2",
                Latitude = 51.502936649999995,
                Longitude = 0.0032029278126681844
            },
            new()
            {
                Title = "Future Event 3",
                Date = DateTime.UtcNow.AddMonths(3),
                Description = "Event 3 months in future",
                Category = "drinks",
                City = "London",
                Venue = "The Mayflower",
                Latitude = 51.501778,
                Longitude = -0.053577
            },
            new()
            {
                Title = "Future Event 4",
                Date = DateTime.UtcNow.AddMonths(4),
                Description = "Event 4 months in future",
                Category = "drinks",
                City = "London",
                Venue = "The Blackfriday",
                Latitude = 51.512146650000005,
                Longitude = -0.10364680647106028
            },
            new()
            {
                Title = "Future Event 5",
                Date = DateTime.UtcNow.AddMonths(5),
                Description = "Event 5 months in future",
                Category = "culture",
                City = "London",
                Venue =
                    "Sherlock Holmes Museum, 221b, Baker Street, Marylebone, London, Greater London, England, NW1 6XE, United Kingdom",
                Latitude = 51.5237629,
                Longitude = -0.1584743
            },
            new()
            {
                Title = "Future Event 6",
                Date = DateTime.UtcNow.AddMonths(6),
                Description = "Event 6 months in future",
                Category = "music",
                City = "London",
                Venue =
                    "Roundhouse, Chalk Farm Road, Maitland Park, Chalk Farm, London Borough of Camden, London, Greater London, England, NW1 8EH, United Kingdom",
                Latitude = 51.5432505,
                Longitude = -0.15197608174931165
            },
            new()
            {
                Title = "Future Event 7",
                Date = DateTime.UtcNow.AddMonths(7),
                Description = "Event 2 months ago",
                Category = "travel",
                City = "London",
                Venue = "River Thames, England, United Kingdom",
                Latitude = 51.5575525,
                Longitude = -0.781404
            },
            new()
            {
                Title = "Future Event 8",
                Date = DateTime.UtcNow.AddMonths(8),
                Description = "Event 8 months in future",
                Category = "film",
                City = "London",
                Venue = "River Thames, England, United Kingdom",
                Latitude = 51.5575525,
                Longitude = -0.781404
            }
        };
            context.Events.AddRange(events);
            
            
            await context.SaveChangesAsync();
    }
}