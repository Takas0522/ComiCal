using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ComiCal.Batch.Models;
using ComiCal.Batch.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Functions
{
    public class Orchestration
    {
        private readonly IComicService _comicService;
        public Orchestration(
            IComicService comicService
        )
        {
            _comicService = comicService;
        }

        [FunctionName("Orchestration")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log
        )
        {
            var pageCount = await context.CallActivityAsync<int>("GetPageCount", "");
            log.LogInformation($"Get PageCount Result={pageCount}");

            for (int i = 1; i <= pageCount; i++)
            {
                await context.CallActivityAsync("WaitTime", 15);
                await context.CallActivityAsync("Register", i);
            }

            log.LogInformation($"Data Get Complete");
            var updateIageUrls = await context.CallActivityAsync<IEnumerable<ComicImage>>("GetUpdateImageTarget", "");
            log.LogInformation($"Update Image ${updateIageUrls.Count()}");

            foreach (var updateIageUrl in updateIageUrls)
            {
                await context.CallActivityAsync("WaitTime", 5);
                await context.CallActivityAsync("UpdateImage", updateIageUrl);
            }
        }

        [FunctionName("GetPageCount")]
        public async Task<int> GetPageCount ([ActivityTrigger] string val, ILogger log)
        {
            return await _comicService.GetPageCountAsync();
        }

        [FunctionName("GetUpdateImageTarget")]
        public async Task<IEnumerable<ComicImage>> GetUpdateImageTarget([ActivityTrigger] string val, ILogger log)
        {
            return await _comicService.GetUpdateImageTargetAsync();
        }

        [FunctionName("UpdateImage")]
        public async Task UpdateImage([ActivityTrigger] ComicImage val, ILogger log)
        {
            await _comicService.UpdateImageDataAsync(val);
        }

        [FunctionName("WaitTime")]
        public async Task WaitTime([ActivityTrigger] int waitTimeSec, ILogger log)
        {
            await Task.Delay(waitTimeSec * 1000);
        }

        [FunctionName("Register")]
        public async Task Register([ActivityTrigger] int pageCount, ILogger log)
        {
            log.LogInformation($"Run Page: {pageCount}");
            await _comicService.RegitoryAsync(pageCount);
        }

        [FunctionName("TimerStart")]
        public static async Task Run(
            [TimerTrigger("0 0 0 * * *", RunOnStartup = true)] TimerInfo myTimer,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("Orchestration", null);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}