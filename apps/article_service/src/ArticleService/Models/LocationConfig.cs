namespace ArticleService.Models;

public class LocationConfig
{
    public List<Location> Locations { get; set; } = [];
}

public class Location
{
    public required string Code { get; set; }
    public required string Description { get; set; }
    public bool Enabled { get; set; }
}