using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Command.ApexLegends
{

    public class Rootobject
    {
        public Global global { get; set; }
        public Realtime realtime { get; set; }
        public Legends legends { get; set; }
    }

    public class Global
    {
        public string name { get; set; }
        public long uid { get; set; }
        public string avatar { get; set; }
        public string platform { get; set; }
        public int level { get; set; }
        public int toNextLevelPercent { get; set; }
        public int internalUpdateCount { get; set; }
        public Bans bans { get; set; }
        public Rank rank { get; set; }
        public Arena arena { get; set; }
    }

    public class Bans
    {
        public bool isActive { get; set; }
        public int remainingSeconds { get; set; }
        public string last_banReason { get; set; }
    }

    public class Rank
    {
        public int rankScore { get; set; }
        public string rankName { get; set; }
        public int rankDiv { get; set; }
        public int ladderPosPlatform { get; set; }
        public string rankImg { get; set; }
        public string rankedSeason { get; set; }
        public string RankNameCN
        {
            get
            {
                return rankName switch
                {
                    "Bronze"
                        => "青铜",
                    "Silver"
                        => "白银",
                    "Gold"
                        => "黄金",
                    "Platinum"
                        => "铂金",
                    "Diamond"
                        => "钻石",
                    "Master"
                        => "大师",
                    "Apex Predator"
                        => "APEX 猎杀者",
                    _
                        => "未定级",
                };
            }
        }
    }

    public class Arena
    {
        public int rankScore { get; set; }
        public string rankName { get; set; }
        public int rankDiv { get; set; }
        public int ladderPosPlatform { get; set; }
        public string rankImg { get; set; }
        public string rankedSeason { get; set; }
        public string RankNameCN
        {
            get
            {
                return rankName switch
                {
                    "Bronze"
                        => "青铜",
                    "Silver"
                        => "白银",
                    "Gold"
                        => "黄金",
                    "Platinum"
                        => "铂金",
                    "Diamond"
                        => "钻石",
                    "Master"
                        => "大师",
                    "Apex Predator"
                        => "APEX 猎杀者",
                    _
                        => "未定级",
                };
            }
        }
    }

    public class Realtime
    {
        public string lobbyState { get; set; }
        public int isOnline { get; set; }
        public int isInGame { get; set; }
        public int canJoin { get; set; }
        public int partyFull { get; set; }
        public string selectedLegend { get; set; }
        public string currentState { get; set; }
        public int currentStateSinceTimestamp { get; set; }
        public string currentStateAsText { get; set; }
    }

    public class Legends
    {
        public Selected selected { get; set; }
    }

    public class Selected
    {
        public string LegendName { get; set; }
        public object[] data { get; set; }
        public Gameinfo gameInfo { get; set; }
        public Imgassets ImgAssets { get; set; }
    }

    public class Gameinfo
    {
        public string skin { get; set; }
        public string skinRarity { get; set; }
        public string frame { get; set; }
        public string frameRarity { get; set; }
        public string pose { get; set; }
        public string poseRarity { get; set; }
        public string intro { get; set; }
        public string introRarity { get; set; }
        public Badge[] badges { get; set; }
    }

    public class Badge
    {
        public string name { get; set; }
        public int value { get; set; }
        public string category { get; set; }
    }

    public class Imgassets
    {
        public string icon { get; set; }
        public string banner { get; set; }
    }

}