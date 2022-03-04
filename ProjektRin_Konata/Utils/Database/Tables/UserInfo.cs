using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjektRin.Utils.Database.Tables
{
    [Table("T_USER_INFO")]
    public class UserInfo
    {
        [PrimaryKey]
        [Column("uin")]
        public uint uin { get; set; }

        [Column("coin")]
        public uint coin { get; set; }

        [Column("level")]
        public int level { get; set; }

        [Column("exp")]
        public int exp { get; set; }

        [Column("last_sign")]
        public DateTime lastSign { get; set; }
    }
}
