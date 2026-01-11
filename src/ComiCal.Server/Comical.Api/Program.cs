using Comical.Api.Repositories;
using Comical.Api.Services;
using ComiCal.Shared;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var configuration = builder.Configuration;
builder.Services.AddComicalStartupSharedConfiguration(configuration);
builder.Services.AddSingleton<IComicRepository, ComicRepository>();
builder.Services.AddSingleton<IComicService, ComicService>();
builder.Services.AddSingleton<IConfigMigrationRepository, ConfigMigrationRepository>();
builder.Services.AddSingleton<IConfigMigrationService, ConfigMigrationService>();

// Configure JSON serialization options globally
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNameCaseInsensitive = true;
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Build().Run();
