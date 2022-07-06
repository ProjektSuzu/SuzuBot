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
        public string Value { get; set; }
    }
}
