using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Comical.Api.Models
{
    public class GetComicsRequest
    {
        [JsonPropertyName("searchList")]
        public IEnumerable<string>? SearchList { get; set; }
    }
}
