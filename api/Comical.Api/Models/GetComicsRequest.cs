using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Comical.Api.Models
{
    public class GetComicsRequest
    {
        [DataMember(Name = "searchList")]
        public IEnumerable<string> SearchList { get; set; }
    }
}
