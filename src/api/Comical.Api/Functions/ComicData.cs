using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Comical.Api.Services;
using Comical.Api.Models;
using Comical.Api.Util.Common;

namespace Comical.Api
{
    public class ComicData
    {
        private readonly IComicService _comicService;
        private readonly ILogger<ComicData> _logger;

        public ComicData(
            IComicService comicService,
            ILogger<ComicData> logger
        )
        {
            _comicService = comicService;
            _logger = logger;
        }

        [Function("ComicData")]
        public async Task<HttpResponseData> GetComicData(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            return await FunctionExecutionHelper.ExecuteAsync(
                req,
                _logger,
                async () =>
                {
                    var query = req.Query["fromdate"];
                    var fromdate = DateTime.UtcNow.AddMonths(-1);
                    if (!string.IsNullOrEmpty(query))
                    {
                        fromdate = DateTime.Parse(query).ToUniversalTime();
                    }

                    var data = await req.ReadFromJsonAsync<GetComicsRequest>();

                    var comics = await _comicService.GetComicsAsync(data!, fromdate);

                    return await HttpResponseHelper.CreateOkResponseAsync(req, comics);
                });
        }
    }
}
