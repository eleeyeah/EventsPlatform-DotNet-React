namespace Domain;

public class Event
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string Title { get; init; }
    public DateTime Date { get; init; }
    public bool IsCancelled { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    
    // location props
    public required string City { get; init; }
    public required string Venue { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}