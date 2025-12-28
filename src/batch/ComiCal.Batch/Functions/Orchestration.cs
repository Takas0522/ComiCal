using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ComiCal.Batch.Services;
using ComiCal.Shared.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
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

            // Step 1: Register comic data for all pages
            for (int i = 1; i <= pageCount; i++)
            {
                await context.CallActivityAsync("WaitTime", 15);
                await context.CallActivityAsync("Register", i);
            }

            log.LogInformation($"Data Get Complete");

            // Step 2: Download images for all pages
            log.LogInformation($"Starting image download process for {pageCount} pages");
            for (int i = 1; i <= pageCount; i++)
            {
                await context.CallActivityAsync("WaitTime", 15);
                await context.CallActivityAsync("DownloadImages", i);
            }

            log.LogInformation($"Image download complete");
        }

        [FunctionName("GetPageCount")]
        public async Task<int> GetPageCount ([ActivityTrigger] string val, ILogger log)
        {
            return await _comicService.GetPageCountAsync();
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

        [FunctionName("DownloadImages")]
        public async Task DownloadImages([ActivityTrigger] int pageNumber, ILogger log)
        {
            log.LogInformation($"Downloading images for page: {pageNumber}");
            await _comicService.ProcessImageDownloadsAsync(pageNumber);
        }

        [FunctionName("TimerStart")]
        public static async Task Run(
            [TimerTrigger(
                "0 0 0 * * *"
#if DEBUG
            , RunOnStartup=true
#endif
            )] TimerInfo myTimer,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("Orchestration", null);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}