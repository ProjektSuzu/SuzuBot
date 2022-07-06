using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Core.Component.Database
{
    [Table("T_ENV_VAR")]
    internal class EnvironmentVariable
    {
        [PrimaryKey]
        [Column("key")]
        public string Key { get; set; }

        [Column("value")]
        public string Value { get; set; }
    }
}
