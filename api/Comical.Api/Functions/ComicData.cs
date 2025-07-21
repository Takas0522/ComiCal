using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
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

        [Function("ComicData")]
        public async Task<HttpResponseData> GetComicData(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req,
            ILogger log)
        {
            var fromdate = DateTime.Now.AddMonths(-1);
            
            // Parse query parameters
            var query = req.Query;
            var fromdateQuery = query["fromdate"];
            if (!string.IsNullOrEmpty(fromdateQuery))
            {
                fromdate = DateTime.Parse(fromdateQuery);
            }

            // Read request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<GetComicsRequest>(requestBody);

            var d = await _comicService.GetComicsAsync(data, fromdate);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(JsonSerializer.ToJsonString(d));
            
            return response;
        }
    }
}
