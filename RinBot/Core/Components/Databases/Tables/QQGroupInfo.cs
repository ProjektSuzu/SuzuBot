using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Core.Components.Databases.Tables
{
    [Table("T_QQ_GROUP_INFO")]
    internal class QQGroupInfo
    {
        [PrimaryKey]
        [Column("uin")]
        public uint Uin { get; set; }

        [Column("inviter_uin")]
        public uint InviterUin { get; set; }

        [Column("module_ids")]
        public string ModuleIdsJson { get; set; }

        [Ignore]
        public List<string> ModuleIds
        {
            get
            {
                if (ModuleIdsJson == null)
                    return new();
                return JsonConvert.DeserializeObject<List<string>>(ModuleIdsJson) ?? new();
            }
            set
            {
                ModuleIdsJson = JsonConvert.SerializeObject(value);
            }
        }
    }
}
