using Comical.Api.Providers;
using Comical.Api.Repositories;
using Comical.Api.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

[assembly: FunctionsStartup(typeof(ComiCal.Api.Startup))]
namespace ComiCal.Api
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Register ConnectionProvider for PostgreSQL connection string management
            builder.Services.AddSingleton<ConnectionProvider>();

            builder.Services.AddSingleton<IComicRepository, ComicRepository>();
            builder.Services.AddSingleton<IComicService, ComicService>();
            builder.Services.AddSingleton<IConfigMigrationRepository, ConfigMigrationRepository>();
            builder.Services.AddSingleton<IConfigMigrationService, ConfigMigrationService>();
        }
    }
}
