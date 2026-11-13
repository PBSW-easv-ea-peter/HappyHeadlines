namespace ArticleService.Models;

public class LocationConfig
{
    public List<Location> Locations { get; set; } = [];
}

public class Location
{
    public string Code { get; set; }
    public string Description { get; set; }
    public bool Enabled { get; set; }
}