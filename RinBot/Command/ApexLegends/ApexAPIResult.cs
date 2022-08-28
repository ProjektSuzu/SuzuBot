using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Command.ApexLegends
{

    public class PredatorResult
    {
        public RP RP { get; set; }
        public AP AP { get; set; }
        public string Error { get; set; }
    }

    public class RP
    {
        public PC PC { get; set; }
    }

    public class PC
    {
        public int foundRank { get; set; }
        public int val { get; set; }
        public string uid { get; set; }
        public int updateTimestamp { get; set; }
        public int totalMastersAndPreds { get; set; }
    }

    public class AP
    {
        public PC1 PC { get; set; }
    }

    public class PC1
    {
        public int foundRank { get; set; }
        public int val { get; set; }
        public string uid { get; set; }
        public int updateTimestamp { get; set; }
        public int totalMastersAndPreds { get; set; }
    }

    public class PlayerInfoResult
    {
        public string Error { get; set; }
        public Global global { get; set; }
        public Realtime realtime { get; set; }
        public Legends legends { get; set; }
        public Club club { get; set; }
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
    }

    public class Arena
    {
        public int rankScore { get; set; }
        public string rankName { get; set; }
        public int rankDiv { get; set; }
        public int ladderPosPlatform { get; set; }
        public string rankImg { get; set; }
        public string rankedSeason { get; set; }
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
        public Data[] data { get; set; }
        public Gameinfo gameInfo { get; set; }
        public Imgassets ImgAssets { get; set; }
    }


    public class Data
    {
        public string name { get; set; }
        public int value { get; set; }
        public string key { get; set; }
        public DataRank rank { get; set; }
        public Rankplatformspecific rankPlatformSpecific { get; set; }
    }

    public class DataRank
    {
        public int rankPos { get; set; }
        public float topPercent { get; set; }
    }

    public class Rankplatformspecific
    {
        public string rankPos { get; set; }
        public string topPercent { get; set; }
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

    public class Club
    {
        public string id { get; set; }
        public string name { get; set; }
        public string tag { get; set; }
        public long createdByUID { get; set; }
        public int groupSize { get; set; }
        public int maxGroupSize { get; set; }
        public string datacenter { get; set; }
        public string logo { get; set; }
        public Joinconfig joinConfig { get; set; }
        public Member[] members { get; set; }
    }

    public class Joinconfig
    {
        public bool canUserRequestMembership { get; set; }
        public bool isFreeJoin { get; set; }
        public bool isPwdProtected { get; set; }
        public bool canInviteToJoin { get; set; }
    }

    public class Member
    {
        public long linkedOrigin { get; set; }
        public long uid { get; set; }
        public string platform { get; set; }
        public string name { get; set; }
        public long memberSince { get; set; }
        public string role { get; set; }
        public int roleId { get; set; }
    }


}