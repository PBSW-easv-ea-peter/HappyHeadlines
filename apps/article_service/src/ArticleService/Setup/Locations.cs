using ArticleService.Models;

namespace ArticleService.Setup;

public static class Locations
{
    extension(WebApplicationBuilder builder)
    {
        public void ConfigureLocations()
        {
            builder.Services.Configure<LocationConfig>(
                builder.Configuration.GetSection("ArticleLocations")
            );
        }
    }
}