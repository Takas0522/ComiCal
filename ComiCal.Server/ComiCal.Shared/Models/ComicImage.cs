using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComiCal.Shared.Models
{
    public class ComicImage
    {
        public string Isbn { get; set; } = "";
        public string ImageBaseUrl { get; set; } = "";
        public string? ImageStorageUrl { get; set; }
    }
}
