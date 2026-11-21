using System.Net;
using System.Net.Http.Json;
using Domain;
using Xunit;

namespace EventsPlatform.Tests;

/// <summary>Behaviour of GET /api/events over HTTP.</summary>
public class EventsControllerTests : IClassFixture<EventsPlatformFactory>
{
    private readonly HttpClient _client;

    public EventsControllerTests(EventsPlatformFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task The_list_returns_the_seeded_events()
    {
        var events = await _client.GetFromJsonAsync<List<Event>>("/api/events");

        Assert.Equal(10, events!.Count);
        Assert.Contains(events, e => e.Title == "Future Event 1");
    }

    [Fact]
    public async Task An_event_can_be_fetched_by_its_id()
    {
        var all = await _client.GetFromJsonAsync<List<Event>>("/api/events");
        var first = all!.First();

        var response = await _client.GetAsync($"/api/events/{first.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await response.Content.ReadFromJsonAsync<Event>();
        Assert.Equal(first.Id, fetched!.Id);
        Assert.Equal(first.Title, fetched.Title);
    }

    [Fact]
    public async Task An_unknown_event_id_returns_404()
    {
        var response = await _client.GetAsync("/api/events/no-such-event");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Each_event_carries_its_location()
    {
        var events = await _client.GetFromJsonAsync<List<Event>>("/api/events");
        var paris = events!.Single(e => e.Title == "Past Event 2");

        Assert.Equal("Paris", paris.City);
        Assert.Equal("Louvre Museum", paris.Venue.Split(',')[0]);
        Assert.InRange(paris.Latitude, 48.0, 49.0);
    }
}
