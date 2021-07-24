using Comical.Api.Repositories;
using Comical.Api.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
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
            builder.Services.AddSingleton<IComicRepository, ComicRepository>();
            builder.Services.AddSingleton<IComicService, ComicService>();
        }
    }
}
