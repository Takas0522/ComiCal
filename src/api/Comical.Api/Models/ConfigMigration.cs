using System;
using System.Collections.Generic;
using System.Text;

namespace Comical.Api.Models
{
    public class ConfigMigration
    {
        public string id { get; set; } = "";
        public string Id
        {
            get => id;
            set => id = value;
        }
        public string Value { get; set; } = "";
    }
}
