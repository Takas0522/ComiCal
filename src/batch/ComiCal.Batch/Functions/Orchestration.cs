using System;
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
            [OrchestrationTrigger] TaskOrchestrationContext context
        )
        {
            var log = context.CreateReplaySafeLogger<Orchestration>();
            log.LogDebug("Orchestration started");
            
            var pageCount = await context.CallActivityAsync<int>("GetPageCount");
            log.LogInformation("Get PageCount Result={PageCount}", pageCount);

            // Step 1: Register comic data for all pages
            log.LogDebug("Starting registration loop for {PageCount} pages", pageCount);
            for (int i = 1; i <= pageCount; i++)
            {
                await context.CallActivityAsync("WaitTime", 15);
                await context.CallActivityAsync("Register", i);
            }

            log.LogInformation("Data Get Complete");

            // Step 2: Download images for all pages
            log.LogDebug("Starting image download loop");
            log.LogInformation("Starting image download process for {PageCount} pages", pageCount);
            for (int i = 1; i <= pageCount; i++)
            {
                await context.CallActivityAsync("WaitTime", 15);
                await context.CallActivityAsync("DownloadImages", i);
            }

            log.LogInformation("Image download complete");
        }

        [Function("GetPageCount")]
        public async Task<int> GetPageCount([ActivityTrigger] string? input, FunctionContext context)
        {
            var log = context.GetLogger<Orchestration>();
            log.LogDebug("GetPageCount activity started");
            
            try
            {
                var result = await _comicService.GetPageCountAsync();
                log.LogInformation("Successfully retrieved page count: {Count}", result);
                return result;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to get page count");
                throw;
            }
        }

        [Function("WaitTime")]
        public async Task WaitTime([ActivityTrigger] int waitTimeSec, FunctionContext context)
        {
            var log = context.GetLogger<Orchestration>();
            log.LogDebug("WaitTime activity started");
            await Task.Delay(waitTimeSec * 1000);
        }

        [Function("Register")]
        public async Task Register([ActivityTrigger] int pageCount, FunctionContext context)
        {
            var log = context.GetLogger<Orchestration>();
            log.LogInformation("Run Page: {PageCount}", pageCount);
            
            try
            {
                await _comicService.RegitoryAsync(pageCount);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to register page: {PageCount}", pageCount);
                throw;
            }
        }

        [Function("DownloadImages")]
        public async Task DownloadImages([ActivityTrigger] int pageNumber, FunctionContext context)
        {
            var log = context.GetLogger<Orchestration>();
            log.LogInformation("Downloading images for page: {PageNumber}", pageNumber);
            
            try
            {
                await _comicService.ProcessImageDownloadsAsync(pageNumber);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to download images for page: {PageNumber}", pageNumber);
                throw;
            }
        }

        [Function("TimerStart")]
        public static async Task Run(
            [TimerTrigger(
                "0 0 0 * * *",
                RunOnStartup=true
            )] TimerInfo myTimer,
            [DurableClient] DurableTaskClient starter,
            FunctionContext executionContext)
        {
            var log = executionContext.GetLogger("TimerStart");
            log.LogDebug("Timer triggered");
            string instanceId = await starter.ScheduleNewOrchestrationInstanceAsync("Orchestration");
            log.LogInformation("Started orchestration with ID = '{InstanceId}'.", instanceId);
        }
    }
}