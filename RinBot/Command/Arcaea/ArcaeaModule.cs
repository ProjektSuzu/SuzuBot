using Konata.Core.Message;
using RinBot.Command.Arcaea.Database;
using RinBot.Core;
using RinBot.Core.Components.Attributes;
using RinBot.Core.Components.Commands;
using RinBot.Core.KonataCore.Events;
using System.Text;
using System.Xml.Linq;

namespace RinBot.Command.Arcaea
{
    [Module("Arcaea", "AkulaKirov.Arcaea")]
    internal class ArcaeaModule
    {
        internal struct SongQueryArgs
        {
            public string SongName { get; set; } = string.Empty;
            public RatingClass RatingClass { get; set; } = RatingClass.Future;
            public bool HasRatingClass { get; set; } = false;

            public SongQueryArgs(string text)
            {
                var list = text.Split(' ', StringSplitOptions.RemoveEmptyEntries & StringSplitOptions.TrimEntries);
                switch (list.Last().ToLower())
                {
                    case "pst":
                    case "past":
                    case "0":
                        RatingClass = RatingClass.Past;
                        HasRatingClass = true;
                        break;

                    case "prs":
                    case "present":
                    case "1":
                        RatingClass = RatingClass.Present;
                        HasRatingClass = true;
                        break;

                    case "ftr":
                    case "future":
                    case "2":
                        RatingClass = RatingClass.Future;
                        HasRatingClass = true;
                        break;

                    case "byd":
                    case "byn":
                    case "beyond":
                    case "3":
                        RatingClass = RatingClass.Beyond;
                        HasRatingClass = true;
                        break;
                }
                if (HasRatingClass)
                    list = list[0..^1];
                SongName = string.Join(' ', list);
            }
        }
        internal enum QueryResult
        {
            OK,
            NotFound,
            Ambiguous,
        }

        internal static readonly string RESOURCE_DIR_PATH = Path.Combine(GlobalScope.RESOURCE_DIR_PATH, "AkulaKirov.Arcaea");
        internal static readonly string DATABASE_DIR_PATH = Path.Combine(RESOURCE_DIR_PATH, "database");
        internal static readonly string COVER_DIR_PATH = Path.Combine(RESOURCE_DIR_PATH, "cover");

        internal static ArcaeaSongDatabase ArcaeaSongDatabase
            => new ArcaeaSongDatabase();
        internal static ArcaeaUserDatabase ArcaeaUserDatabase
            => new ArcaeaUserDatabase();
        internal static ArcaeaUnlimitedAPI ArcaeaUnlimitedAPI
            => new ArcaeaUnlimitedAPI();
        internal static GraphGenerator GraphGenerator
            => new GraphGenerator();

        public ArcaeaModule()
        {
            Directory.CreateDirectory(RESOURCE_DIR_PATH);
            Directory.CreateDirectory(DATABASE_DIR_PATH);
            Directory.CreateDirectory(COVER_DIR_PATH);
        }

        [TextCommand("Arcaea", new[] { "arcaea", "arc", "a" })]
        public void OnArcaea(MessageEventArgs messageEvent, CommandStruct command)
        {
            if (command.FuncArgs.Length <= 0)
            {
                OnRecent(messageEvent);
                return;
            }
            else
            {
                var subCommand = command.FuncArgs[0];
                command.FuncToken = subCommand;
                command.FuncArgs = command.FuncArgs[1..];
                switch (subCommand)
                {
                    // 别名查询
                    case "alias":
                        {
                            OnAlias(messageEvent, command);
                            return;
                        }

                    // 最佳成绩
                    case "best":
                    case "info":
                        {
                            OnBest(messageEvent, command);
                            return;
                        }

                    // B30
                    case "best30":
                    case "b30":
                        {
                            OnBest30(messageEvent);
                            return;
                        }

                    // 绑定
                    case "bind":
                        {
                            OnBind(messageEvent, command);
                            return;
                        }

                    // 铺面预览
                    case "chart":
                        {
                            OnChartPreview(messageEvent, command);
                            return;
                        }

                    // 最近游玩
                    case "recent":
                    case "r":
                        {
                            OnRecent(messageEvent);
                            return;
                        }

                    // 歌曲信息
                    case "song":
                        {
                            OnSongInfo(messageEvent, command);
                            return;
                        }

                    // 解绑
                    case "unbind":
                        {
                            OnUnbind(messageEvent);
                            return;
                        }

                    default:
                        {
                            messageEvent.Reply($"[Arcaea]\n" +
                                $"找不到功能: {subCommand}");
                            return;
                        }
                }
            }
        }

        public void OnBest30(MessageEventArgs messageEvent)
        {
            var messageBuilder = new MessageBuilder("[Arcaea]Best30\n");
            var bindInfo = ArcaeaUserDatabase.GetBindInfo(messageEvent.Sender.Uin).Result;
            if (bindInfo == null)
            {
                messageBuilder.Text("未存在绑定记录\n" +
                    "请先使用\n" +
                    "/arc bind <userName/userCode>\n" +
                    "进行绑定\n" +
                    "使用例:\n" +
                    "/arc bind YajuuSenpai\n" +
                    "或\n" +
                    "/arc bind 114514810");
                messageEvent.Reply(messageBuilder);
                return;
            }

            var best30Result = ArcaeaUnlimitedAPI.GetBest30Result(bindInfo.UserCode).Result;
            if (best30Result == null)
            {
                messageBuilder.Text($"无法连接到服务器");
                messageEvent.Reply(messageBuilder);
                return;
            }
            else if (best30Result.Status != 0)
            {
                var status = ArcaeaUnlimitedAPI.GetStatus(best30Result.Status);
                messageBuilder.Text(status.Translation);
                messageEvent.Reply(messageBuilder);
                return;
            }
            else
            {
                var accountInfo = best30Result.Content.AccountInfo;
                ArcaeaUserDatabase.UpdateQueryRecord(accountInfo.UserCode, DateTime.Now, (float)accountInfo.Rating / 100);
                var bytes = GraphGenerator.GenerateBest30(best30Result);
                messageBuilder.Image(bytes);
                messageEvent.Reply(messageBuilder);
                return;
            }
        }
        public void OnRecent(MessageEventArgs messageEvent)
        {
            var messageBuilder = new MessageBuilder("[Arcaea]Recent\n");
            var bindInfo = ArcaeaUserDatabase.GetBindInfo(messageEvent.Sender.Uin).Result;
            if (bindInfo == null)
            {
                messageBuilder.Text("未存在绑定记录\n" +
                    "请先使用\n" +
                    "/arc bind <userName/userCode>\n" +
                    "进行绑定\n" +
                    "使用例:\n" +
                    "/arc bind YajuuSenpai\n" +
                    "或\n" +
                    "/arc bind 114514810");
                messageEvent.Reply(messageBuilder);
                return;
            }

            var playerInfoResult = ArcaeaUnlimitedAPI.GetPlayerInfoByCode(bindInfo.UserCode).Result;
            if (playerInfoResult == null)
            {
                messageBuilder.Text($"无法连接到服务器");
                messageEvent.Reply(messageBuilder);
                return;
            }
            else if (playerInfoResult.Status != 0)
            {
                var status = ArcaeaUnlimitedAPI.GetStatus(playerInfoResult.Status);
                messageBuilder.Text(status.Translation);
                messageEvent.Reply(messageBuilder);
                return;
            }
            else
            {
                var accountInfo = playerInfoResult.Content.AccountInfo;
                ArcaeaUserDatabase.UpdateQueryRecord(accountInfo.UserCode, DateTime.Now, (float)accountInfo.Rating / 100);
                var bytes = GraphGenerator.GeneratePlayerResult(playerInfoResult);
                messageBuilder.Image(bytes);
                messageEvent.Reply(messageBuilder);
                return;
            }
        }
        public void OnBest(MessageEventArgs messageEvent, CommandStruct command)
        {
            var messageBuilder = new MessageBuilder("[Arcaea]Best\n");
            var bindInfo = ArcaeaUserDatabase.GetBindInfo(messageEvent.Sender.Uin).Result;
            if (bindInfo == null)
            {
                messageBuilder.Text("未存在绑定记录\n" +
                    "请先使用\n" +
                    "/arc bind <userName/userCode>\n" +
                    "进行绑定\n" +
                    "使用例:\n" +
                    "/arc bind YajuuSenpai\n" +
                    "或\n" +
                    "/arc bind 114514810");
                messageEvent.Reply(messageBuilder);
                return;
            }

            var queryArgs = new SongQueryArgs(string.Join(' ', command.FuncArgs));
            List<Chart> charts;
            var result = TryQueryCharts(queryArgs, out charts);
            switch (result)
            {
                case QueryResult.OK:
                    {
                        var songId = charts[0].SongId;
                        var bestPlayInfo = ArcaeaUnlimitedAPI.GetBestResult(bindInfo.UserCode, songId, queryArgs.RatingClass).Result;
                        if (bestPlayInfo == null)
                        {
                            messageBuilder.Text($"无法连接到服务器");
                        }
                        else if (bestPlayInfo.Status != 0)
                        {
                            var status = ArcaeaUnlimitedAPI.GetStatus(bestPlayInfo.Status);
                            messageBuilder.Text(status.Translation);
                        }
                        else
                        {
                            var bytes = GraphGenerator.GeneratePlayerResult(bestPlayInfo);
                            messageBuilder.Image(bytes);
                        }
                        break;
                    }
                case QueryResult.NotFound:
                    {
                        messageBuilder.Text($"找不到歌曲: {queryArgs.SongName}");
                        break;
                    }
                case QueryResult.Ambiguous:
                    {
                        List<string> list = new();
                        while (charts.Count > 0)
                        {
                            var chart = charts.First();
                            list.Add(chart.NameEN);
                            if (chart.NameJP != string.Empty)
                                list.Add(chart.NameJP);
                            charts.RemoveAll(x => x.SongId == chart.SongId);
                        }
                        StringBuilder stringBuilder = new();
                        stringBuilder.AppendLine($"关键字 {queryArgs.SongName} 具有二义性:");
                        int count = list.Count;
                        if (count > 5)
                            list = list.Take(5).ToList();
                        foreach (var name in list)
                        {
                            stringBuilder.AppendLine(name);
                        }
                        stringBuilder.AppendLine($"等 {count} 个结果\n请尝试补全关键字");
                        messageBuilder.Text(stringBuilder.ToString());
                        break;
                    }
            }
            messageEvent.Reply(messageBuilder);
        }
        public void OnBind(MessageEventArgs messageEvent, CommandStruct command)
        {
            var messageBuilder = new MessageBuilder("[Arcaea]Bind\n");
            var bindInfo = ArcaeaUserDatabase.GetBindInfo(messageEvent.Sender.Uin).Result;
            //if (bindInfo != null)
            //{
            //    var playerInfo = ArcaeaUserDatabase.GetPlayerInfo(bindInfo.UserCode).Result;
            //    messageBuilder.Text("已存在绑定记录:\n" +
            //        $"{playerInfo.UserName}({playerInfo.UserCode})\n" +
            //        $"如需解除绑定 请使用:\n" +
            //        $"/arc unbind");
            //    messageEvent.Reply(messageBuilder);
            //    return;
            //}
            if (command.FuncArgs.Length <= 0)
            {
                messageBuilder.Text("缺少参数: <userName/userCode>");
                messageEvent.Reply(messageBuilder);
                return;
            }
            var userToken = command.FuncArgs[0];
            var userInfo = ArcaeaUnlimitedAPI.GetPlayerInfoByCode(userToken).Result;
            if (userInfo == null)
            {
                messageBuilder.Text($"无法连接到服务器");
                messageEvent.Reply(messageBuilder);
                return;
            }
            else if (userInfo.Status != 0)
            {
                userInfo = ArcaeaUnlimitedAPI.GetPlayerInfoByName(userToken).Result;
                if (userInfo == null)
                {
                    messageBuilder.Text($"无法连接到服务器");
                    messageEvent.Reply(messageBuilder);
                    return;
                }
                else if (userInfo.Status != 0)
                {
                    var status = ArcaeaUnlimitedAPI.GetStatus(userInfo.Status);
                    messageBuilder.Text(status.Translation);
                    messageEvent.Reply(messageBuilder);
                    return;
                }
            }

            var accountInfo = userInfo.Content.AccountInfo;
            if (bindInfo != null)
                ArcaeaUserDatabase.RemoveBindInfo(bindInfo.Uin);
            ArcaeaUserDatabase.AddBindInfo(messageEvent.Sender.Uin, accountInfo.UserCode);
            if (ArcaeaUserDatabase.GetPlayerInfo(accountInfo.UserCode).Result == null)
                ArcaeaUserDatabase.AddPlayerInfo(accountInfo.UserCode, accountInfo.UserName);
            ArcaeaUserDatabase.UpdateQueryRecord(accountInfo.UserCode, DateTime.Now, (float)accountInfo.Rating / 100);
            if (bindInfo == null)
            {
                messageBuilder.Text($"已绑定:\n{accountInfo.UserName}({accountInfo.UserCode})");
            }
            else
            {
                var playerInfo = ArcaeaUserDatabase.GetPlayerInfo(bindInfo.UserCode).Result;
                messageBuilder.Text($"{playerInfo.UserName}({playerInfo.UserCode})\n绑定已更换:\n{accountInfo.UserName}({accountInfo.UserCode})");
            }
            messageEvent.Reply(messageBuilder);
            return;
        }
        public void OnUnbind(MessageEventArgs messageEvent)
        {
            var messageBuilder = new MessageBuilder("[Arcaea]Unbind\n");
            var bindInfo = ArcaeaUserDatabase.GetBindInfo(messageEvent.Sender.Uin).Result;
            if (bindInfo != null)
            {
                ArcaeaUserDatabase.RemoveBindInfo(messageEvent.Sender.Uin);
                messageBuilder.Text("已解除绑定");
                messageEvent.Reply(messageBuilder);
                return;
            }
            else
            {
                messageBuilder.Text("未存在绑定记录");
                messageEvent.Reply(messageBuilder);
                return;
            }
        }
        public void OnSongInfo(MessageEventArgs messageEvent, CommandStruct command)
        {
            var messageBuilder = new MessageBuilder("[Arcaea]SongInfo\n");
            if (command.FuncArgs.Length <= 0)
            {
                messageBuilder.Text("缺少参数: <keyword>");
                messageEvent.Reply(messageBuilder);
                return;
            }
            var queryArgs = new SongQueryArgs(string.Join(' ', command.FuncArgs));
            List<Chart> charts;
            var result = TryQueryCharts(queryArgs, out charts);
            switch (result)
            {
                case QueryResult.OK:
                    {
                        var first = charts[0];
                        List<string> names = new();
                        charts = charts.OrderBy(x => x.RatingClass).ToList();
                        foreach (var chart in charts)
                        {
                            if (chart.JacketOverride || chart.RatingClass == RatingClass.Past)
                            {
                                messageBuilder.Image(GraphGenerator.GetSongCover(chart));
                            }
                            if (!names.Contains(chart.NameEN))
                                names.Add(chart.NameEN);
                            if (!names.Contains(chart.NameJP))
                                names.Add(chart.NameJP);
                        }
                        var side = first.ChartSide switch
                        {
                            ChartSide.Hikari => "光芒之侧",
                            ChartSide.Conflict => "纷争之侧",
                            ChartSide.Colerless => "消色之侧",
                            _ => "???",
                        };
                        var pack = first.Package.PackageName;
                        StringBuilder stringBuilder = new();
                        stringBuilder.AppendLine($"曲名: \n{string.Join('\n', names)}");
                        stringBuilder.AppendLine($"曲包: {pack} {side}");
                        stringBuilder.AppendLine($"        PST| PRS| FTR{(charts.Count > 3 ? "| BYD " : "")}");
                        stringBuilder.AppendLine($"难度: {String.Join('|', charts.Select(x => x.DifficultyStr.PadLeft(4)).ToList())}");
                        stringBuilder.AppendLine($"定数: {String.Join('|', charts.Select(x => ((float)x.Rating / 10).ToString().PadLeft(4)).ToList())}");
                        stringBuilder.AppendLine($"物量: {String.Join('|', charts.Select(x => x.Note.ToString().PadLeft(4)).ToList())}");
                        stringBuilder.AppendLine($"\n提示: 可使用\n/arc alias {first.SongId}\n来查询该曲的别名");

                        messageBuilder.Text(stringBuilder.ToString());
                        break;
                    }
                case QueryResult.NotFound:
                    {
                        messageBuilder.Text($"找不到歌曲: {queryArgs.SongName}");
                        break;
                    }
                case QueryResult.Ambiguous:
                    {
                        List<string> list = new();
                        while (charts.Count > 0)
                        {
                            var chart = charts.First();
                            list.Add(chart.NameEN);
                            if (chart.NameJP != string.Empty)
                                list.Add(chart.NameJP);
                            charts.RemoveAll(x => x.SongId == chart.SongId);
                        }
                        StringBuilder stringBuilder = new();
                        stringBuilder.AppendLine($"关键字 {queryArgs.SongName} 具有二义性:");
                        int count = list.Count;
                        if (count > 5)
                            list = list.Take(5).ToList();
                        foreach (var name in list)
                        {
                            stringBuilder.AppendLine(name);
                        }
                        stringBuilder.AppendLine($"等 {count} 个结果\n请尝试补全关键字");
                        messageBuilder.Text(stringBuilder.ToString());
                        break;
                    }
            }
            messageEvent.Reply(messageBuilder);
        }
        public void OnChartPreview(MessageEventArgs messageEvent, CommandStruct command)
        {
            var messageBuilder = new MessageBuilder("[Arcaea]ChartPreview\n");
            if (command.FuncArgs.Length <= 0)
            {
                messageBuilder.Text("缺少参数: <keyword>");
                messageEvent.Reply(messageBuilder);
                return;
            }
            var queryArgs = new SongQueryArgs(string.Join(' ', command.FuncArgs));
            List<Chart> charts;
            var result = TryQueryCharts(queryArgs, out charts);
            switch (result)
            {
                case QueryResult.OK:
                    {
                        var songId = charts[0].SongId;
                        var bytes = ArcaeaUnlimitedAPI.GetChartPreview(
                            songId, queryArgs.RatingClass).Result;
                        if (bytes.Length == 0)
                        {
                            messageBuilder.Text($"服务器连接超时或铺面难度不存在: {queryArgs.SongName} {queryArgs.RatingClass}");
                            break;
                        }
                        messageBuilder.Image(bytes);
                        break;
                    }
                case QueryResult.NotFound:
                    {
                        messageBuilder.Text($"找不到歌曲: {queryArgs.SongName}");
                        break;
                    }
                case QueryResult.Ambiguous:
                    {
                        List<string> list = new();
                        while (charts.Count > 0)
                        {
                            var chart = charts.First();
                            list.Add(chart.NameEN);
                            if (chart.NameJP != string.Empty)
                                list.Add(chart.NameJP);
                            charts.RemoveAll(x => x.SongId == chart.SongId);
                        }
                        StringBuilder stringBuilder = new();
                        stringBuilder.AppendLine($"关键字 {queryArgs.SongName} 具有二义性:");
                        int count = list.Count;
                        if (count > 5)
                            list = list.Take(5).ToList();
                        foreach (var name in list)
                        {
                            stringBuilder.AppendLine(name);
                        }
                        stringBuilder.AppendLine($"等 {count} 个结果\n请尝试补全关键字");
                        break;
                    }
            }
            messageEvent.Reply(messageBuilder);
        }
        public void OnAlias(MessageEventArgs messageEvent, CommandStruct command)
        {
            var messageBuilder = new MessageBuilder("[Arcaea]Alias\n");
            if (command.FuncArgs.Length <= 0)
            {
                messageBuilder.Text("缺少参数: <keyword>");
                messageEvent.Reply(messageBuilder);
                return;
            }
            var queryArgs = new SongQueryArgs(string.Join(' ', command.FuncArgs));
            List<Chart> charts;
            var result = TryQueryCharts(queryArgs, out charts);
            switch (result)
            {
                case QueryResult.OK:
                    {
                        var first = charts[0];
                        var alias = ArcaeaSongDatabase.GetAlias(first.SongId).Result;
                        List<string> names = new();
                        charts = charts.OrderBy(x => x.RatingClass).ToList();
                        foreach (var chart in charts)
                        {
                            if (!names.Contains(chart.NameEN))
                                names.Add(chart.NameEN);
                            if (!names.Contains(chart.NameJP))
                                names.Add(chart.NameJP);
                        }
                        var stringBuilder = new StringBuilder();
                        foreach (var name in names)
                            stringBuilder.AppendLine(name);
                        stringBuilder.AppendLine("该曲具有以下别名:");
                        foreach (var alia in alias)
                            stringBuilder.AppendLine(alia.SongAlias);

                        messageBuilder.Text(stringBuilder.ToString());
                        break;
                    }
                case QueryResult.NotFound:
                    {
                        messageBuilder.Text($"找不到歌曲: {queryArgs.SongName}");
                        break;
                    }
                case QueryResult.Ambiguous:
                    {
                        List<string> list = new();
                        while (charts.Count > 0)
                        {
                            var chart = charts.First();
                            list.Add(chart.NameEN);
                            if (chart.NameJP != string.Empty)
                                list.Add(chart.NameJP);
                            charts.RemoveAll(x => x.SongId == chart.SongId);
                        }
                        StringBuilder stringBuilder = new();
                        stringBuilder.AppendLine($"关键字 {queryArgs.SongName} 具有二义性:");
                        int count = list.Count;
                        if (count > 5)
                            list = list.Take(5).ToList();
                        foreach (var name in list)
                        {
                            stringBuilder.AppendLine(name);
                        }
                        stringBuilder.AppendLine($"等 {count} 个结果\n请尝试补全关键字");
                        messageBuilder.Text(stringBuilder.ToString());
                        break;
                    }
            }
            messageEvent.Reply(messageBuilder);
        }
        public QueryResult TryQueryCharts(SongQueryArgs queryArgs, out List<Chart> charts)
        {
            charts = ArcaeaSongDatabase.GetChartsCoarse(queryArgs.SongName).Result;
            if (queryArgs.HasRatingClass)
                charts = charts.Where(x => x.RatingClass == queryArgs.RatingClass).ToList();
            if (charts.Count <= 0) return QueryResult.NotFound;

            var firstChart = charts[0];
            if (charts.Any(x => x.SongId != firstChart.SongId)) return QueryResult.Ambiguous;
            return QueryResult.OK;
        }
    }
}
