using SQLite;

namespace RinBot.Core.Components.Databases.Tables
{
    [Table("T_MODULE_INFO")]
    internal class ModuleInfo
    {
        [PrimaryKey]
        [Column("module_id")]
        public string ModuleId { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("is_enabled")]
        public bool IsEnabled { get; set; }
        [Column("is_critical")]
        public bool IsCritical { get; set; }
    }
}
