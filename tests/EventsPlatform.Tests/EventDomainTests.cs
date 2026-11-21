using Domain;
using Xunit;

namespace EventsPlatform.Tests;

/// <summary>The Event entity: what a new event looks like before it is saved.</summary>
public class EventDomainTests
{
    private static Event NewEvent(string title = "Launch") => new()
    {
        Title = title,
        Date = new DateTime(2030, 1, 1, 18, 0, 0, DateTimeKind.Utc),
        City = "London",
        Venue = "The O2",
    };

    [Fact]
    public void A_new_event_gets_a_non_empty_id()
    {
        Assert.False(string.IsNullOrWhiteSpace(NewEvent().Id));
    }

    [Fact]
    public void Each_new_event_gets_its_own_id()
    {
        Assert.NotEqual(NewEvent().Id, NewEvent().Id);
    }

    [Fact]
    public void A_new_event_is_not_cancelled_and_has_no_optional_details()
    {
        var evt = NewEvent();

        Assert.False(evt.IsCancelled);
        Assert.Null(evt.Description);
        Assert.Null(evt.Category);
    }

    [Fact]
    public void The_title_is_kept_as_given()
    {
        Assert.Equal("Summer Fest", NewEvent("Summer Fest").Title);
    }
}
