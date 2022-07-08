using Newtonsoft.Json;
using SQLite;

namespace RinBot.Core.Component.Database
{
    [Table("T_QQ_GROUP_LIST")]
    internal class QQGroupInfo
    {
        [PrimaryKey]
        [Column("group_id")]
        public uint GroupId { get; set; }

        [Column("inviter_id")]
        public uint InviterId { get; set; }

        [Column("disable_modules")]
        public string DisableModuleIdsJson { get; set; }
        
        [Ignore]
        public List<string> DisableModuleIds
        {
            get
            {
                if (DisableModuleIdsJson == null)
                    return new();
                return JsonConvert.DeserializeObject<List<string>>(DisableModuleIdsJson);
            }
            set
            {
                DisableModuleIdsJson = JsonConvert.SerializeObject(value);
            }
        }

        [Column("white_listed")]
        public bool WhiteListed { get; set; }

        [Column("black_listed")]
        public bool BlackListed { get; set; }

    }
}
