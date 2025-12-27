using ComiCal.Batch.Models;
using ComiCal.Batch.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using ComiCal.Batch.Util.Common;
using System.IO;
using Castle.Core.Logging;
using ComiCal.Shared.Models;

namespace ComiCal.Batch.Services
{
    public class ComicService : IComicService
    {
        private readonly IRakutenComicRepository _rakutenComicRepository;
        private readonly IComicRepository _comicRepository;

        public ComicService(
            IRakutenComicRepository rakutenComicRepository,
            IComicRepository comicRepository
        )
        {
            _rakutenComicRepository = rakutenComicRepository;
            _comicRepository = comicRepository;
        }

        public async Task<int> GetPageCountAsync()
        {
            RakutenComicResponse data = await _rakutenComicRepository.Fetch(1);
            return data.PageCount;
        }

        public async Task RegitoryAsync(int requestPage)
        {
            RakutenComicResponse baseData = await _rakutenComicRepository.Fetch(requestPage);
            IEnumerable<Comic> comics = GenRegistData(baseData);

            await _comicRepository.UpsertComicsAsync(comics);
        }

        private IEnumerable<Comic> GenRegistData(RakutenComicResponse data)
        {
            return data.Comics.Select(x =>
            {
                var date = DateTimeUtility.JpDateToDateTimeType(x.Info.SalesDate);
                return new Comic
                {
                    Author = x.Info.Author,
                    AuthorKana = x.Info.AuthorKana,
                    Isbn = x.Info.Isbn,
                    PublisherName = x.Info.PublisherName,
                    SalesDate = date.value,
                    SeriesName = x.Info.SeriesName,
                    SeriesNameKana = x.Info.SeriesNameKana,
                    Title = x.Info.Title,
                    TitleKana = x.Info.TitleKana,
                    ScheduleStatus = (int)date.status
                };
            });
        }
    }
}
