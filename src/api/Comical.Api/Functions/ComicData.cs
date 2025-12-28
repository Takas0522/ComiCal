using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Comical.Api.Services;
using Utf8Json;
using Comical.Api.Models;

namespace Comical.Api
{
    public class ComicData
    {
        private readonly IComicService _comicService;

        public ComicData(
            IComicService comicService
        )
        {
            _comicService = comicService;
        }

        [FunctionName("ComicData")]
        public async Task<IActionResult> GetComicData(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var query = req.Query["fromdate"];
            var fromdate = DateTime.UtcNow.AddMonths(-1);
            if (query.Count !=0 && query != string.Empty)
            {
                fromdate = DateTime.Parse(query).ToUniversalTime();
            }
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<GetComicsRequest>(requestBody);

            var d = await _comicService.GetComicsAsync(data, fromdate);

            return new OkObjectResult(d);
        }
    }
}
