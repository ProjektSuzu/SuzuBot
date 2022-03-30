using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using Newtonsoft.Json;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using System.Text.RegularExpressions;

namespace ProjektRin.Commands.Modules.Arcaea
{
    [CommandSet("Arcaea", "com.akulak.arcaea")]
    internal class ArcaeaCommand : BaseCommand
    {
        private readonly ArcaeaUnlimitedAPI aua = ArcaeaUnlimitedAPI.Instance;
        private readonly ArcUserInfoDB arcUserDB = ArcUserInfoDB.Instance;

        public override string Help => $"[Arcaea]\n" +
                $"/arc      打印帮助信息\n" +
                $"/arc b30 [<usercode>]       获取b30成绩图\n" +
                $"/arc recent [<usercode>]          获取最近一次游玩成绩图\n" +
                $"/arc best <song> <PST/PRS/FTR/BYD>    获取一首歌的最佳游玩记录\n" +
                $"/arc bind <name/usercode>      为当前QQ号绑定好友代码\n" +
                $"/arc unbind       为当前QQ号解绑好友代码\n" +
                $"/arc suggest [<min>]      根据当前B30结果来推荐能推分的歌曲\n" +
                $"/arc info <song>  查询一首歌的信息\n" +
                $"\n" +
                $"  name        Arcaea玩家名字\n" +
                $"  usercode    Arcaea好友代码 必须是9位纯数字\n" +
                $"  song        歌曲名字 可以是歌曲全名、内部sid、或者别名\n" +
                $"  min         推荐歌曲的最小推分值 例如 0.01\n";

        public override void OnInit()
        {

        }

        [GroupMessageCommand("Arcaea", new[] { @"^arc\s?([\s\S]+)?", @"^a\s?([\s\S]+)?" })]
        public void OnArcaea(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string? funcName = args.FirstOrDefault();
            string? reply = "";

            if (funcName == null)
            {
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(Help));
                return;
            }

            args = args.Skip(1).ToList();
            switch (funcName)
            {
                case "b30":
                    {
                        OnB30(bot, messageEvent, args);
                        break;
                    }

                case "bind":
                    {
                        OnBind(bot, messageEvent, args);
                        break;
                    }

                case "unbind":
                    {
                        OnUnbind(bot, messageEvent, args);
                        break;
                    }

                case "song":
                case "best":
                    {
                        OnSongBest(bot, messageEvent, args);
                        break;
                    }

                case "info":
                    {
                        OnSongInfo(bot, messageEvent, args);
                        break;
                    }

                case "suggest":
                case "推荐":
                case "推分":
                    {
                        OnSongSuggest(bot, messageEvent, args);
                        break;
                    }

                case "r":
                case "recent":
                    {
                        OnRecent(bot, messageEvent, args);
                        break;
                    }

                default:
                    {
                        reply = $"错误: 找不到功能: \"{funcName}\"\n" +
                            $"如需查看功能帮助 请输入\n" +
                            $"/arc";
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                        return;
                    }
            }
        }

        private void OnRecent(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string? reply = "";
            string? usercode = "";

            if (args.Count > 0)
            {
                usercode = args[0];
            }

            if (usercode != "")
            {
                Regex? regex = new Regex(@"^[0-9]{9}$");
                Match? match = regex.Match(usercode);
                if (!match.Success)
                {
                    reply = $"错误: 参数非法: \"{usercode}\" => [<usercode>].";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
            }
            else
            {
                ArcaeaUserInfo? info = arcUserDB.GetByUin(messageEvent.MemberUin);
                if (info == null)
                {
                    reply =
                        $"错误: 当前QQ号不存在绑定的记录.\n" +
                        $"若要使用此功能, 请先使用 /arc bind <name/usercode> 进行绑定\n";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
                else
                {
                    usercode = info.UserCode;
                }
            }

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                .Add(ReplyChain.Create(messageEvent.Message))
                .Text("收到, 正在处理成绩图...")
                );

            UserInfoResult? result = aua.GetUserInfo(usercode).Result;

            if (result == null)
            {
                reply = $"错误: 获取失败";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text(reply)
                    );
                return;
            }

            if (result.status != 0)
            {
                reply = $"错误: {result.message}";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text(reply)
                    );
                return;
            }

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Image(GraphGenerator.Instance.GeneratePlayResult(result))
                    );
            return;
        }


        private void OnSongBest(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string? usercode = "";
            string? reply = "";
            List<string>? songName = new List<string>();
            int difficulty = -1;
            while (args.Count > 0)
            {
                string? arg = args[0];
                args.RemoveAt(0);

                string? argUpper = arg.ToUpper();

                switch (argUpper)
                {
                    case "PST":
                        difficulty = 0;
                        break;

                    case "PRS":
                        difficulty = 1;
                        break;

                    case "FTR":
                        difficulty = 2;
                        break;

                    case "BYD":
                        difficulty = 3;
                        break;

                    default:
                        songName.Add(arg);
                        break;
                }
            }
            string? sid = string.Join(' ', songName);

            if (difficulty == -1)
            {
                reply = $"错误: 缺少参数: <PST/PRS/FTR/BYD>.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            if (sid == "")
            {
                reply = $"错误: 缺少参数: <song>.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            ArcaeaUserInfo? info = arcUserDB.GetByUin(messageEvent.MemberUin);
            if (info == null)
            {
                reply =
                    $"错误: 当前QQ号不存在绑定的记录.\n" +
                    $"若要使用此功能, 请先使用 /arc bind <name/usercode> 进行绑定\n";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }
            else
            {
                usercode = info.UserCode;
            }

            Song? song = ArcSongDB.Instance.TryGetSong(sid);
            if (song == null)
            {
                reply = $"错误: 找不到歌曲: \"{sid}\"";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                .Add(ReplyChain.Create(messageEvent.Message))
                .Text("\n收到, 正在处理成绩图...")
                );

            BestPlayResult? result = aua.GetUserBest(usercode, song.SongID, (SongResult.Difficulty)difficulty).Result;

            if (result == null)
            {
                reply = $"错误: 获取失败";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text(reply)
                    );
                return;
            }

            if (result.status != 0)
            {
                reply = $"错误: {result.message}";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text(reply)
                    );
                return;
            }

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Image(GraphGenerator.Instance.GeneratePlayResult(result))
                    );
            return;
        }

        private async void OnSongSuggest(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string? reply = "";
            string? usercode = "";
            float min = 0.001f;

            if (args.Count > 0)
            {
                if (!float.TryParse(args[0], out min))
                {
                    reply = $"错误: 参数非法: \"{args[0]}\" [<min>].";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
            }

            ArcaeaUserInfo? info = arcUserDB.GetByUin(messageEvent.MemberUin);
            if (info == null)
            {
                reply =
                    $"错误: 当前QQ号不存在绑定的记录.\n" +
                    $"若要使用此功能, 请先使用 /arc bind <name/usercode> 进行绑定\n";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }
            else
            {
                usercode = info.UserCode;
            }

            B30Result b30;

            reply =
                $"处理中 请稍候..";
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply).Add(ReplyChain.Create(messageEvent.Message)));

            try
            {
                string? json = info.B30Json;
                if (json == null || json == "")
                {
                    b30 = aua.GetB30(usercode).Result;
                    if (b30 == null || b30.status != 0)
                    {
                        reply = $"错误: {b30.message}";
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text(reply)
                            );
                        return;
                    }
                }
                else
                {
                    b30 = JsonConvert.DeserializeObject<B30Result>(json);
                    if (b30 == null || b30.status != 0)
                    {
                        reply = $"错误: {b30.message}";
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text(reply)
                            );
                        return;
                    }
                }
            }
            catch
            {
                reply = $"错误: 获取失败";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text(reply)
                    );
                return;
            }

            SongSuggester.SuggestResult? result = SongSuggester.Suggest(b30, min);

            if (result == null)
            {
                reply =
                    $"没有找到适合你的歌曲...";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text(reply));
                return;
            }

            string GetScore(SongSuggester.TargetScore score)
            {
                switch (score)
                {
                    case SongSuggester.TargetScore.S950W:
                        return "950W";

                    case SongSuggester.TargetScore.S980W:
                        return "980W";

                    case SongSuggester.TargetScore.S990W:
                        return "990W";

                    case SongSuggester.TargetScore.S995W:
                        return "995W";

                    case SongSuggester.TargetScore.S1000W:
                        return "1000W";

                    default:
                        return "PM";
                }
            }

            reply =
                    $"推荐歌曲: {result.Song.NameEN}\n" +
                    $"该歌曲 {result.Difficulty} 难度定数为 {SongSuggester.GetRating(result.Song, result.Difficulty)}\n" +
                    $"若你将其推至 {GetScore(result.TargetScore)} 分\n" +
                    $"预计B30平均值将增加 {result.B30Delta:0.0000}\n" +
                    $"" +
                    $"{(result.IsOverRank ? "\n⚠警告: 该曲对你当前PTT有越级风险⚠\n" : "")}" +
                    $"\n" +
                    $"B30数据更新时间: {info.LastUpdate:G}";
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                .Add(ReplyChain.Create(messageEvent.Message))
                .Image(GraphGenerator.Instance.GetCoverBmp(result.Song.SongID, result.Difficulty == SongResult.Difficulty.Beyond).Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 80).ToArray())
                .Text(reply));
            return;
        }

        private void OnSongInfo(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string? reply = "";
            if (args.Count == 0)
            {
                reply = $"错误: 缺少参数: <song>.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            string? sid = string.Join(' ', args);

            Song? song = ArcSongDB.Instance.TryGetSong(sid);
            if (song == null)
            {
                reply = $"错误: 找不到歌曲: \"{sid}\"";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            reply =
                $"曲名: {song.NameEN}\n" +
                $"BPM: {song.BPM}\n" +
                $"  PST/PRS/FTR/BYD\n" +
                $"定数: {(float)song.RatingPST / 10:0.0}/{(float)song.RatingPRS / 10:0.0}/{(float)song.RatingFTR / 10:0.0}/{(song.RatingBYD < 0 ? "-" : ((float)song.RatingBYD / 10).ToString("0.0"))}\n" +
                $"物量: {song.NotePST}/{song.NotePRS}/{song.NoteFTR}/{(song.NoteBYD < 0 ? "-" : song.NoteBYD.ToString())}\n";

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                .Add(ReplyChain.Create(messageEvent.Message))
                .Image(GraphGenerator.Instance.GetCoverBmp(song.SongID).Encode(SkiaSharp.SKEncodedImageFormat.Png, 100).ToArray())
                .Text(reply));
        }

        private void OnB30(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string? reply = "";
            string? usercode = "";

            string? arg = "";
            while (args.Count > 0)
            {
                arg = args[0];
                args.RemoveAt(0);

                switch (arg)
                {
                    default:
                        {
                            usercode = arg;
                            break;
                        }
                }
            }

            if (usercode != "")
            {
                Regex? regex = new Regex(@"^[0-9]{9}$");
                Match? match = regex.Match(usercode);
                if (!match.Success)
                {
                    reply = $"错误: 参数非法: \"{usercode}\" => [<usercode>].\n" +
                        $"请检查是否意外的添加了空格和括号.";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
            }
            else
            {
                ArcaeaUserInfo? info = arcUserDB.GetByUin(messageEvent.MemberUin);
                if (info == null)
                {
                    reply =
                        $"错误: 当前QQ号不存在绑定的记录.\n" +
                        $"若要使用此功能, 请先使用 /arc bind <name/usercode> 进行绑定\n" +
                        $"或者直接使用 /arc b30 [<usercode>] 并指定 [<usercode>].";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
                else
                {
                    usercode = info.UserCode;
                }
            }

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                .Add(ReplyChain.Create(messageEvent.Message))
                .Text("收到, 正在处理成绩图...")
                );

            B30Result? result = null;

            int retry = 3;
            for (int i = 0; i < retry; i++)
            {
                result = aua.GetB30(usercode).Result;
                if (result != null)
                {
                    break;
                }
            }

            if (result == null)
            {
                reply = $"错误: 获取失败\n" +
                    $"如果你是第一次获取B30成绩图 请等几分钟后再试.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text(reply)
                    );
                return;
            }

            if (result.status != 0)
            {
                reply = $"错误: {result.message}";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text(reply)
                    );
                return;
            }

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Image(GraphGenerator.Instance.GenerateBest30(result))
                    );
            return;
        }

        private void OnUnbind(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string? reply = "";
            ArcaeaUserInfo? info = arcUserDB.GetByUin(messageEvent.MemberUin);
            if (info == null)
            {
                reply = "错误: 当前QQ号不存在绑定的记录.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }
            else
            {
                arcUserDB.Remove(messageEvent.MemberUin);
                reply = $"U{messageEvent.MemberUin} => ∅    解绑成功.\n";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply).Add(ReplyChain.Create(messageEvent.Message)));
                return;
            }
        }

        private void OnBind(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string? reply = "";
            if (args.Count == 0)
            {
                reply = "错误: 缺少参数: <name/usercode>.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }
            ArcaeaUserInfo? info = arcUserDB.GetByUin(messageEvent.MemberUin);
            if (info != null)
            {
                reply = "错误: 当前QQ号已存在一个绑定的记录.\n" +
                    "如需更换绑定, 请先使用 /arc unbind 解绑.\n" +
                    $"U{info.Uin} => {info.UserName}({info.UserCode}).";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply).Add(ReplyChain.Create(messageEvent.Message)));
                return;
            }

            string? user = args[0];

            UserInfoResult? result = aua.GetUserInfo(user).Result;

            if (result == null)
            {
                reply = $"错误: 获取失败";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text(reply)
                    );
                return;
            }

            if (result.content == null)
            {
                result = aua.GetUserInfoByName(user).Result;
            }

            if (result == null)
            {
                reply = $"错误: 获取失败";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text(reply)
                    );
                return;
            }

            if (result.content == null)
            {
                reply = $"错误: 未能查找到对应用户信息: {user}.\n" +
                    $"{result.message}.\n" +
                    $"请检查是否意外的添加了空格和括号.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply).Add(ReplyChain.Create(messageEvent.Message)));
                return;
            }

            ArcaeaUserInfo? newInfo = new ArcaeaUserInfo() { Uin = messageEvent.MemberUin, UserName = result.content.account_info.name, UserCode = result.content.account_info.code, B30Json = "" };

            arcUserDB.Insert(newInfo);
            reply = $"绑定成功\n" +
                $"U{messageEvent.MemberUin} => {newInfo.UserName}({newInfo.UserCode}).";
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply).Add(ReplyChain.Create(messageEvent.Message)));
            return;
        }
    }
}
