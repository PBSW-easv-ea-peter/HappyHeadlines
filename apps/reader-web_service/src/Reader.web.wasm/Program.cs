using System;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Reader.web.wasm;
using ReaderPages.web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using ReaderPages.web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// NOTE: CreateDefault already loaded config over HTTP from:
//   wwwroot/appsettings.json
//   wwwroot/appsettings.{Environment}.json  (e.g. appsettings.Development.json)
// No ConfigurationBuilder / SetBasePath needed — WASM has no filesystem.

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddLogging();
builder.Services.AddMudServices();

// Read settings straight from builder.Configuration
string articleServiceUrl = builder.Configuration["ServiceUrls:ArticleService"]
                           ?? throw new InvalidOperationException(
                               "Missing config key 'ServiceUrls:ArticleService'. " +
                               "Make sure appsettings.json is in wwwroot and set 'Copy to Output Directory' if needed.");

string commentServiceUrl = builder.Configuration["ServiceUrls:CommentService"]
                           ?? throw new InvalidOperationException(
                               "Missing config key 'ServiceUrls:CommentService'.");

// Register named HttpClients with base URLs from configuration
builder.Services.AddHttpClient("ArticleService", client =>
{
    client.BaseAddress = new Uri(articleServiceUrl);
});

builder.Services.AddHttpClient("CommentService", client =>
{
    client.BaseAddress = new Uri(commentServiceUrl);
});

// Initialize static configuration for SiteUrls
SiteUrls.Initialize(builder.Configuration);

await builder.Build().RunAsync();