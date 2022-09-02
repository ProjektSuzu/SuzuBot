using Newtonsoft.Json;
using SQLite;
using System.Collections.Generic;
using System.Globalization;

namespace RinBot.Command.Arcaea.Database
{
    internal class ArcaeaUserDatabase
    {
        public ArcaeaUserDatabase()
        {
            dbConnection = new(ARCAEA_USER_DB_PATH);
            dbConnection.CreateTableAsync<ArcaeaBindInfo>();
            dbConnection.CreateTableAsync<ArcaeaPlayerInfo>();
        }

        private static string ARCAEA_USER_DB_PATH => Path.Combine(ArcaeaModule.DATABASE_DIR_PATH, "arcaea.db");
        private SQLiteAsyncConnection dbConnection;
        public SQLiteAsyncConnection DBConnection
            => dbConnection;

        public async Task<ArcaeaBindInfo?> GetBindInfo(uint uin)
        {
            return await dbConnection.Table<ArcaeaBindInfo>()
                .Where(x => x.Uin == uin)
                .FirstOrDefaultAsync();
        }
        public async Task<ArcaeaPlayerInfo?> GetPlayerInfo(string userCode)
        {
            return await dbConnection.Table<ArcaeaPlayerInfo>()
                .Where(x => x.UserCode == userCode)
                .FirstOrDefaultAsync();
        }
        public bool AddBindInfo(uint uin, string usercode)
        {
            return dbConnection
                .InsertAsync(new ArcaeaBindInfo() { Uin = uin, UserCode = usercode }).Result > 0;
        }
        public bool RemoveBindInfo(uint uin)
        {
            return dbConnection
                .DeleteAsync<ArcaeaBindInfo>(uin).Result > 0;
        }
        public bool AddPlayerInfo(string userCode, string userName)
        {
            return dbConnection
                .InsertAsync(new ArcaeaPlayerInfo() { UserCode = userCode, UserName = userName, QueryRecords = new() }).Result > 0;
        }
        public bool UpdatePlayerInfo(ArcaeaPlayerInfo playerInfo)
        {
            return dbConnection.UpdateAsync(playerInfo).Result > 0;
        }
        public bool UpdateQueryRecord(string userCode, DateTime dateTime, float potential)
        {
            if (potential < 0)
                return false;
            var playerInfo = GetPlayerInfo(userCode).Result;
            if (playerInfo == null) return false;

            var latest = playerInfo.QueryRecords.OrderByDescending(x => x.DateTime).FirstOrDefault();
            if (latest != null && dateTime - latest.DateTime <= TimeSpan.FromDays(1))
            {
                playerInfo.QueryRecords.Remove(latest);
                latest.DateTime = dateTime;
                latest.Potential = potential;
                playerInfo.QueryRecords.Add(latest);
            }
            else
            {
                var newRecords = playerInfo.QueryRecords;
                newRecords.Add
                    (
                        new(dateTime, potential)
                    );
                playerInfo.QueryRecords = newRecords;
            }
            return UpdatePlayerInfo(playerInfo);
        }
    }

    [Table("T_BIND_INFO")]
    internal class ArcaeaBindInfo
    {
        [PrimaryKey]
        [Column("uin")]
        public uint Uin { get; set; }
        [Column("user_code")]
        public string UserCode { get; set; }
    }
    [Table("T_PLAYER_INFO")]
    internal class ArcaeaPlayerInfo
    {
        [PrimaryKey]
        [Column("user_code")]
        public string UserCode { get; set; }
        [Column("user_name")]
        public string UserName { get; set; }
        [Column("query_record")]
        public string QueryRecordStr { get; set; } = "[]";
        [Ignore]
        public List<QueryRecord> QueryRecords
        {
            get
            {
                var list = JsonConvert.DeserializeObject<List<string>>(QueryRecordStr) ?? new();
                if (list.Count <= 0) return new();
                List<QueryRecord> result = new();
                foreach (var str in list)
                {
                    var record = QueryRecord.Create(str);
                    if (record != null)
                    {
                        result.Add(record);
                    }
                }
                return result;
            }
            set
            {
                var list = value.Select(x => x.ToString()).ToList();
                QueryRecordStr = JsonConvert.SerializeObject(list);
            }
        }
    }
    internal class QueryRecord
    {
        public DateTime DateTime { get; set; }
        public float Potential { get; set; }

        public static QueryRecord? Create(string text)
        {

            var datetime = DateTime.ParseExact(text[..8], "yyyyMMdd", CultureInfo.InvariantCulture);
            if (float.TryParse(text.Substring(8, 4), out var potential))
            {
                potential /= 100;
                return new QueryRecord(datetime, potential);
            }
            else
            {
                return null;
            }
        }

        public QueryRecord(DateTime dateTime, float potential)
        {
            DateTime = dateTime;
            Potential = potential;
        }

        public override string ToString()
        {
            return $"{DateTime:yyyyMMdd}{((int)(Potential * 100)).ToString().PadLeft(4, '0')}";
        }
    }
}
