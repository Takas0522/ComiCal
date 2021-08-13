using ComiCal.Batch.Repositories;
using ComiCal.Batch.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

[assembly: FunctionsStartup(typeof(ComiCal.Batch.Startup))]
namespace ComiCal.Batch
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<IRakutenComicRepository, RakutenComicRepository>();
            builder.Services.AddSingleton<IComicRepository, ComicRepository>();
            builder.Services.AddSingleton<IComicService, ComicService>();
            builder.Services.AddSingleton<IComicImageRepository, ComicImageRepository>();
        }
    }
}
