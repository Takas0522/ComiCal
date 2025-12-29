using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ComiCal.Batch.Services;
using ComiCal.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
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

        [Function("Orchestration")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context,
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

        [Function("GetPageCount")]
        public async Task<int> GetPageCount ([ActivityTrigger] string val, FunctionContext executionContext)
        {
            return await _comicService.GetPageCountAsync();
        }

        [Function("WaitTime")]
        public async Task WaitTime([ActivityTrigger] int waitTimeSec, FunctionContext executionContext)
        {
            await Task.Delay(waitTimeSec * 1000);
        }

        [Function("Register")]
        public async Task Register([ActivityTrigger] int pageCount, FunctionContext executionContext)
        {
            var log = executionContext.GetLogger("Register");
            log.LogInformation($"Run Page: {pageCount}");
            await _comicService.RegitoryAsync(pageCount);
        }

        [Function("DownloadImages")]
        public async Task DownloadImages([ActivityTrigger] int pageNumber, FunctionContext executionContext)
        {
            var log = executionContext.GetLogger("DownloadImages");
            log.LogInformation($"Downloading images for page: {pageNumber}");
            await _comicService.ProcessImageDownloadsAsync(pageNumber);
        }

        [Function("TimerStart")]
        public static async Task Run(
            [TimerTrigger(
                "0 0 0 * * *"
#if DEBUG
            , RunOnStartup=true
#endif
            )] TimerInfo myTimer,
            [DurableClient] DurableTaskClient starter,
            FunctionContext executionContext)
        {
            var log = executionContext.GetLogger("TimerStart");
            string instanceId = await starter.ScheduleNewOrchestrationInstanceAsync("Orchestration");
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}