using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            
            var pageCount = await context.CallActivityAsync<int>("GetPageCount");

            // Step 1: Register comic data for all pages
            log.LogInformation("Starting registration loop for {PageCount} pages", pageCount);
            for (int i = 1; i <= pageCount; i++)
            {
                // Wait 120 seconds between API calls to avoid rate limiting and resource exhaustion
                if (i > 1)
                {
                    await context.CreateTimer(
                        context.CurrentUtcDateTime.AddSeconds(120),
                        CancellationToken.None
                    );
                }
                
                // Retry logic with exponential backoff
                bool success = false;
                int retryCount = 0;
                const int maxRetries = 3;
                
                while (!success && retryCount < maxRetries)
                {
                    try
                    {
                        await context.CallActivityAsync("Register", i);
                        success = true;
                        log.LogInformation("Completed registration for page {Page}/{Total}", i, pageCount);
                    }
                    catch (Exception)
                    {
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            var waitSeconds = 120 * Math.Pow(2, retryCount - 1);
                            log.LogWarning("Registration failed for page {Page}, retrying in {Seconds}s (attempt {Retry}/{Max})", 
                                i, waitSeconds, retryCount, maxRetries);
                            await context.CreateTimer(
                                context.CurrentUtcDateTime.AddSeconds(waitSeconds),
                                CancellationToken.None
                            );
                        }
                        else
                        {
                            log.LogError("Registration failed for page {Page} after {Max} attempts, skipping", i, maxRetries);
                        }
                    }
                }
            }

            log.LogInformation("Data registration complete");

            // Step 2: Download images for all pages
            for (int i = 1; i <= pageCount; i++)
            {
                // Wait 120 seconds between image downloads
                if (i > 1)
                {
                    await context.CreateTimer(
                        context.CurrentUtcDateTime.AddSeconds(120),
                        CancellationToken.None
                    );
                }
                
                // Retry logic with exponential backoff
                bool success = false;
                int retryCount = 0;
                const int maxRetries = 3;
                
                while (!success && retryCount < maxRetries)
                {
                    try
                    {
                        await context.CallActivityAsync("DownloadImages", i);
                        success = true;
                        log.LogInformation("Completed image download for page {Page}/{Total}", i, pageCount);
                    }
                    catch (Exception)
                    {
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            var waitSeconds = 120 * Math.Pow(2, retryCount - 1);
                            log.LogWarning("Image download failed for page {Page}, retrying in {Seconds}s (attempt {Retry}/{Max})", 
                                i, waitSeconds, retryCount, maxRetries);
                            await context.CreateTimer(
                                context.CurrentUtcDateTime.AddSeconds(waitSeconds),
                                CancellationToken.None
                            );
                        }
                        else
                        {
                            log.LogError("Image download failed for page {Page} after {Max} attempts, skipping", i, maxRetries);
                        }
                    }
                }
            }

            log.LogInformation("Image download complete");
        }

        [Function("GetPageCount")]
        public async Task<int> GetPageCount([ActivityTrigger] string? input, FunctionContext context)
        {
            var log = context.GetLogger<Orchestration>();
            log.LogInformation("GetPageCount activity started");
            
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
            log.LogDebug("Downloading images for page: {PageNumber}", pageNumber);
            
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
                "0 0 0 * * *"
            )] TimerInfo myTimer,
            [DurableClient] DurableTaskClient starter,
            FunctionContext executionContext)
        {
            var log = executionContext.GetLogger<Orchestration>();

            var isRunningInAzure = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));
            var utcNow = DateTime.UtcNow;
            var isScheduledUtcWindow = utcNow.Hour == 0 && utcNow.Minute < 5;

            if (isRunningInAzure && !isScheduledUtcWindow)
            {
                log.LogInformation(
                    "TimerStart invoked during host startup; skipping outside scheduled window. UtcNow={UtcNow}",
                    utcNow
                );
                return;
            }

            log.LogInformation("TimerStart triggered. UtcNow={UtcNow}", utcNow);
            string instanceId = await starter.ScheduleNewOrchestrationInstanceAsync("Orchestration");
            log.LogInformation("Started orchestration with ID = '{InstanceId}'.", instanceId);
        }
    }
}