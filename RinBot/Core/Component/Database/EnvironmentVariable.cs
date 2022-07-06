using Newtonsoft.Json;
using SQLite;

namespace RinBot.Core.Component.Database
{
    [Table("T_ENV_VAR")]
    internal class EnvironmentVariable
    {
        [PrimaryKey]
        [Column("key")]
        public string Key { get; set; }

        [Column("value")]
        private string ValueStr { get; set; }

        [Ignore]
        public List<string> Value
        {
            get
            {
                return JsonConvert.DeserializeObject<List<string>>(ValueStr) ?? new();
            }

            set
            {
                ValueStr = JsonConvert.SerializeObject(value);
            }
        }
    }
}
