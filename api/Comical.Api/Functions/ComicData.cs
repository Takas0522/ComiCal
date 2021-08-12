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
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<GetComicsRequest>(requestBody);

            var d = await _comicService.GetComics(data);

            return new OkObjectResult(d);
        }
    }
}
