using System;
using System.Collections.Generic;
using System.Text;

namespace Comical.Api.Models
{
    public class ConfigMigrationGetResponse
    {
        public IEnumerable<string> Data { get; set; }
    }

    public class ConfigMigrationPostResponse
    {
        public string Id { get; set; }
    }
}
