using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComiCal.Shared.Models
{
    public class Comic
    {
        public string Isbn { get; set; } = "";
        public string Title { get; set; } = "";
        public string TitleKana { get; set; } = "";
        public string SeriesName { get; set; } = "";
        public string SeriesNameKana { get; set; } = "";
        public string Author { get; set; } = "";
        public string AuthorKana { get; set; } = "";
        public string PublisherName { get; set; } = "";
        public DateTime SalesDate { get; set; }
        public int ScheduleStatus { get; set; }
        public string? ImageStorageUrl { get; set; }
    }

    public enum ScheduleStatus
    {
        Confirm,
        UntilDay,
        UntilMonth,
        UntilYear,
        Undecided = 9
    }
}