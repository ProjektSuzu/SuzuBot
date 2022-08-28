using Konata.Core.Message;
using Newtonsoft.Json;
using RinBot.Command.ApexLegends.Database;
using RinBot.Core;
using RinBot.Core.Components.Attributes;
using RinBot.Core.Components.Commands;
using RinBot.Core.KonataCore.Events;
using System.Text;

namespace RinBot.Command.ApexLegends
{
    [Module("ApexLegends", "AkulaKirov.ApexLegends")]
    internal class ApexModule
    {
        internal static readonly string RESOURCE_DIR_PATH = Path.Combine(GlobalScope.RESOURCE_DIR_PATH, "AkulaKirov.ApexLegends");
        internal static readonly string DATABASE_DIR_PATH = Path.Combine(RESOURCE_DIR_PATH, "database");
        private static readonly string NAME_TRANSLATION_PATH = Path.Combine(RESOURCE_DIR_PATH, "translations.json");

        public ApexModule()
        {
            Directory.CreateDirectory(RESOURCE_DIR_PATH);
            Directory.CreateDirectory(DATABASE_DIR_PATH);
            if (!File.Exists(RESOURCE_DIR_PATH))
            {
                legendNameTable = new();
            }
            else
            {
                legendNameTable = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(RESOURCE_DIR_PATH))
                           ?? new();
                File.WriteAllTextAsync(RESOURCE_DIR_PATH, JsonConvert.SerializeObject(legendNameTable));
            }
        }

        internal static ApexUserDatabase ApexUserDatabase
            => new ApexUserDatabase();
        internal static ApexAPI ApexAPI
            => new ApexAPI();
        private Dictionary<string, string> legendNameTable;

        [TextCommand("Apex", "apex")]
        public void OnApex(MessageEventArgs messageEvent, CommandStruct command)
        {
            if (command.FuncArgs.Length <= 0)
            {
                OnStatus(messageEvent, command);
                return;
            }
            else
            {
                var subCommand = command.FuncArgs[0];
                command.FuncToken = subCommand;
                command.FuncArgs = command.FuncArgs[1..];
                switch (subCommand)
                {
                    // 绑定
                    case "bind":
                        {
                            OnBind(messageEvent, command);
                            return;
                        }

                    // 查询猎杀
                    case "predator":
                    case "猎杀":
                    case "冲猎":
                        {
                            OnPredator(messageEvent);
                            return;
                        }

                    // 查询玩家信息
                    case "status":
                    case "stat":
                        {
                            OnStatus(messageEvent, command);
                            return;
                        }
                    default:
                        {
                            messageEvent.Reply($"[ApexLegends]\n" +
                                $"找不到功能: {subCommand}");
                            return;
                        }
                }
            }
        }

        public void OnStatus(MessageEventArgs messageEvent, CommandStruct command)
        {
            var messageBuilder = new MessageBuilder("[ApexLegends]Status\n");
            string playerName;
            PlayerInfoResult result;
            if (command.FuncArgs.Length > 0)
            {
                playerName = string.Join(' ', command.FuncArgs);
                result = ApexAPI.GetPlayerInfoByNameAsync(playerName).Result;
            }
            else
            {
                var bindInfo = ApexUserDatabase.GetBindInfo(messageEvent.Sender.Uin).Result;
                if (bindInfo == null)
                {
                    messageBuilder.Text("未存在绑定记录\n" +
                    "请先使用\n" +
                    "/apex bind <userName>\n" +
                    "进行绑定 或直接使用\n" +
                    "/apex status <userName>\n" +
                    "直接查询\n" +
                    "使用例:\n" +
                    "/apex bind 田所浩二\n" +
                    "/apex status 田所浩二");
                    messageEvent.Reply(messageBuilder);
                    return;
                }
                result = ApexAPI.GetPlayerInfoByUIDAsync(bindInfo.UserId).Result;
            }
            if (result == null)
            {
                messageBuilder.Text($"服务器连接超时");
                messageEvent.Reply(messageBuilder);
                return;
            }
            else if (result.Error != null)
            {
                messageBuilder.Text($"查询时发生错误\n{result.Error}\n建议使用 Origin 用户名进行查询");
                messageEvent.Reply(messageBuilder);
                return;
            }
            else
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"用户名: {result.global.name}");
                if (result.club != null)
                {
                    stringBuilder.AppendLine($"战队: [{result.club.tag}]{result.club.name}");
                }
                stringBuilder.AppendLine($"等级: {result.global.level}");
                string onlineStatus;
                if (result.realtime.isOnline == 1)
                {
                    if (result.realtime.isInGame == 1)
                    {
                        onlineStatus = "正在游戏";
                    }
                    else
                    {
                        onlineStatus = "在线";
                    }
                }
                else
                {
                    onlineStatus = "离线或仅限邀请";
                }
                stringBuilder.AppendLine($"在线状态: {onlineStatus}");
                if (result.global.bans.isActive)
                {
                    stringBuilder.AppendLine("===封禁中===");
                    stringBuilder.AppendLine($"封禁原因: {result.global.bans.last_banReason}");
                    var time = new TimeSpan(0, 0, result.global.bans.remainingSeconds);
                    stringBuilder.AppendLine($"剩余时间: {time:dd\\天\\ hh\\时\\ mm\\分\\ ss\\秒}");
                    stringBuilder.AppendLine("===========");
                }
                stringBuilder.AppendLine($"排位段位: {GetRankNameCN(result.global.rank.rankName)} {(result.global.rank.rankDiv == 0 ? "" : result.global.rank.rankDiv)}");
                stringBuilder.AppendLine($"排位积分: {result.global.rank.rankScore} RP");
                stringBuilder.AppendLine($"竞技场段位: {GetRankNameCN(result.global.arena.rankName)} {(result.global.arena.rankDiv == 0 ? "" : result.global.arena.rankDiv)}");
                stringBuilder.AppendLine($"竞技场积分: {result.global.arena.rankScore} RP");
                stringBuilder.AppendLine("================");
                stringBuilder.AppendLine($"当前选择传奇: {GetLegendNameCN(result.legends.selected.LegendName)}");
                if (result.legends.selected.data != null)
                {
                    stringBuilder.AppendLine("追踪器");
                    foreach (var data in result.legends.selected.data)
                    {
                        stringBuilder.AppendLine($"{data.name} {data.value}");
                    }
                }
                messageBuilder.Text(stringBuilder.ToString());
                messageEvent.Reply(messageBuilder);
                return;
            }
        }
        public void OnBind(MessageEventArgs messageEvent, CommandStruct command)
        {
            var messageBuilder = new MessageBuilder("[ApexLegends]Bind\n");
            if (command.FuncArgs.Length <= 0)
            {
                messageBuilder.Text("缺少参数: <userName>");
                messageEvent.Reply(messageBuilder);
                return;
            }
            var userToken = string.Join(' ', command.FuncArgs);
            var result = ApexAPI.GetPlayerInfoByNameAsync(userToken).Result;
            if (result == null)
            {
                messageBuilder.Text($"服务器连接超时");
                messageEvent.Reply(messageBuilder);
                return;
            }
            else if (result.Error != null)
            {
                messageBuilder.Text($"查询时发生错误\n{result.Error}\n建议使用 Origin 用户名进行查询");
                messageEvent.Reply(messageBuilder);
                return;
            }
            else
            {
                var info = ApexUserDatabase.GetBindInfo(messageEvent.Sender.Uin).Result;
                if (info != null)
                {
                    info.UserId = result.global.uid.ToString();
                    ApexUserDatabase.UpdateBindInfo(info);
                }
                else
                {
                    ApexUserDatabase.AddBindInfo(messageEvent.Sender.Uin, result.global.uid.ToString());
                }
                messageBuilder.Text($"已绑定: {result.global.name}\n\n");

                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"用户名: {result.global.name}");
                if (result.club != null)
                {
                    stringBuilder.AppendLine($"战队: [{result.club.tag}]{result.club.name}");
                }
                stringBuilder.AppendLine($"等级: {result.global.level}");
                string onlineStatus;
                if (result.realtime.isOnline == 1)
                {
                    if (result.realtime.isInGame == 1)
                    {
                        onlineStatus = "正在游戏";
                    }
                    else
                    {
                        onlineStatus = "在线";
                    }
                }
                else
                {
                    onlineStatus = "离线或仅限邀请";
                }
                stringBuilder.AppendLine($"在线状态: {onlineStatus}");
                if (result.global.bans.isActive)
                {
                    stringBuilder.AppendLine("===封禁中===");
                    stringBuilder.AppendLine($"封禁原因: {result.global.bans.last_banReason}");
                    var time = new TimeSpan(0, 0, result.global.bans.remainingSeconds);
                    stringBuilder.AppendLine($"剩余时间: {time:dd\\天\\ hh\\时\\ mm\\分\\ ss\\秒}");
                    stringBuilder.AppendLine("===========");
                }
                stringBuilder.AppendLine($"排位段位: {GetRankNameCN(result.global.rank.rankName)} {(result.global.rank.rankDiv == 0 ? "" : result.global.rank.rankDiv)}");
                stringBuilder.AppendLine($"排位积分: {result.global.rank.rankScore} RP");
                stringBuilder.AppendLine($"竞技场段位: {GetRankNameCN(result.global.arena.rankName)} {(result.global.arena.rankDiv == 0 ? "" : result.global.arena.rankDiv)}");
                stringBuilder.AppendLine($"竞技场积分: {result.global.arena.rankScore} RP");
                stringBuilder.AppendLine("================");
                stringBuilder.AppendLine($"当前选择传奇: {GetLegendNameCN(result.legends.selected.LegendName)}");
                if (result.legends.selected.data != null)
                {
                    stringBuilder.AppendLine("追踪器");
                    foreach (var data in result.legends.selected.data)
                    {
                        stringBuilder.AppendLine($"{data.name} {data.value}");
                    }
                }
                messageBuilder.Text(stringBuilder.ToString());
                messageEvent.Reply(messageBuilder);
                return;
            }
        }
        public void OnPredator(MessageEventArgs messageEvent)
        {
            var messageBuilder = new MessageBuilder("[ApexLegends]Predator\n");
            var result = ApexAPI.GetPredatorAsync().Result;
            if (result == null)
            {
                messageBuilder.Text($"服务器连接超时");
                messageEvent.Reply(messageBuilder);
                return;
            }
            else if (result.Error != null)
            {
                messageBuilder.Text($"查询时发生错误\n{result.Error}");
                messageEvent.Reply(messageBuilder);
                return;
            }
            else
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"排位猎杀要求分数: {result.RP.PC.val} RP");
                stringBuilder.AppendLine($"竞技场猎杀要求分数: {result.AP.PC.val} AP");
                messageBuilder.Text(stringBuilder.ToString());
                messageEvent.Reply(messageBuilder);
                return;
            }
        }

        public string GetRankNameCN(string rankName)
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
        public string GetLegendNameCN(string legendName)
        {
            return legendNameTable.TryGetValue(legendName, out var nameCN) ? nameCN : legendName;
        }
    }
}
