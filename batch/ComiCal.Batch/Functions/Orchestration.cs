using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ComiCal.Batch.Services;
using ComiCal.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.DurableTask;
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
        public static async Task RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var logger = context.CreateReplaySafeLogger("Orchestration");
            
            var pageCount = await context.CallActivityAsync<int>("GetPageCount", "");
            logger.LogInformation($"Get PageCount Result={pageCount}");

            for (int i = 1; i <= pageCount; i++)
            {
                await context.CallActivityAsync("WaitTime", 15);
                await context.CallActivityAsync("Register", i);
            }

            logger.LogInformation($"Data Get Complete");
            var updateImageUrls = await context.CallActivityAsync<IEnumerable<ComicImage>>("GetUpdateImageTarget", "");
            logger.LogInformation($"Update Image {updateImageUrls.Count()}");

            foreach (var updateImageUrl in updateImageUrls)
            {
                await context.CallActivityAsync("WaitTime", 5);
                await context.CallActivityAsync("UpdateImage", updateImageUrl);
            }
        }

        [Function("GetPageCount")]
        public async Task<int> GetPageCount([ActivityTrigger] string val, ILogger log)
        {
            return await _comicService.GetPageCountAsync();
        }

        [Function("GetUpdateImageTarget")]
        public async Task<IEnumerable<ComicImage>> GetUpdateImageTarget([ActivityTrigger] string val, ILogger log)
        {
            return await _comicService.GetUpdateImageTargetAsync();
        }

        [Function("UpdateImage")]
        public async Task UpdateImage([ActivityTrigger] ComicImage val, ILogger log)
        {
            await _comicService.UpdateImageDataAsync(val);
        }

        [Function("WaitTime")]
        public async Task WaitTime([ActivityTrigger] int waitTimeSec, ILogger log)
        {
            await Task.Delay(waitTimeSec * 1000);
        }

        [Function("Register")]
        public async Task Register([ActivityTrigger] int pageCount, ILogger log)
        {
            log.LogInformation($"Run Page: {pageCount}");
            await _comicService.RegitoryAsync(pageCount);
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
            ILogger log)
        {
            string instanceId = await starter.ScheduleNewOrchestrationInstanceAsync("Orchestration");
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}