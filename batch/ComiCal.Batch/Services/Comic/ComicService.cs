using ComiCal.Batch.Models;
using ComiCal.Batch.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using ComiCal.Batch.Util.Common;
using System.IO;

namespace ComiCal.Batch.Services
{
    public class ComicService : IComicService
    {
        private readonly IRakutenComicRepository _rakutenComicRepository;
        private readonly IComicRepository _comicRepository;
        private readonly IComicImageRepository _comicImageRepository;

        public ComicService(
            IRakutenComicRepository rakutenComicRepository,
            IComicRepository comicRepository,
            IComicImageRepository comicImageRepository
        )
        {
            _rakutenComicRepository = rakutenComicRepository;
            _comicRepository = comicRepository;
            _comicImageRepository = comicImageRepository;
        }

        public async Task<int> GetPageCountAsync()
        {
            RakutenComicResponse data = await _rakutenComicRepository.Fetch(1);
            return data.PageCount;
        }

        public async Task<IEnumerable<ComicImage>> GetUpdateImageTargetAsync()
        {
            return await _comicRepository.GetUpdateImageTargetAsync();
        }

        public async Task RegitoryAsync(int requestPage)
        {
            RakutenComicResponse baseData = await _rakutenComicRepository.Fetch(requestPage);
            IEnumerable<Comic> comics = GenRegistData(baseData);
            IEnumerable<ComicImage> comicImages = GenRegistImageData(baseData);

            foreach (var comicImage in comicImages)
            {
                var comigImageInfo = await _comicRepository.GetComicImageInfo(comicImage.Isbn);
                if (comigImageInfo != null && comigImageInfo.ImageBaseUrl != null && comigImageInfo.ImageBaseUrl != comicImage.ImageBaseUrl)
                {
                    Uri baseUri = new Uri(comigImageInfo.ImageBaseUrl);
                    await _comicImageRepository.DeleteImageAsync(baseUri.LocalPath);
                }
            }

            await _comicRepository.RegisterComicsAsync(comics, comicImages);
        }

        public async Task UpdateImageDataAsync(ComicImage data)
        {
            BinaryData streamData = await _rakutenComicRepository.FetchImageAndConvertStream(data.ImageBaseUrl);
            Uri uri = new Uri(data.ImageBaseUrl);
            string path = uri.LocalPath;

            await _comicImageRepository.UploadImageAsync(path, streamData);
            await _comicRepository.RegisterComicImageUrlAsync(data.Isbn, path);
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

        private IEnumerable<ComicImage> GenRegistImageData(RakutenComicResponse data)
        {
            return data.Comics.Select(x => {
                return new ComicImage
                {
                    Isbn = x.Info.Isbn,
                    ImageBaseUrl = x.Info.LargeImageUrl
                };
            });
        }
    }
}
