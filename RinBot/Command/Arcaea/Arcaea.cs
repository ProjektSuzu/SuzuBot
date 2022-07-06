using RinBot.Core.Component.Command.CustomAttribute;
using RinBot.Core.Component.Event;
using RinBot.Core.Component.Message;
using RinBot.Core.Component.Message.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Command.Arcaea
{
    [Module("Arcaea", "org.akulak.arcaea")]
    internal class Arcaea
    {
        public Arcaea()
        {
            var instance = ArcaeaSongDB.Instance;
        }

        [Command("Arcaea", new[] { @"^arc\s?(.+)?", @"^a\s(.+)?", @"^a$" }, (int)MatchingType.Regex, ReplyType.Reply)]
        public RinMessageChain OnArcaea(RinEvent e, List<string> args)
        {
            if (args.Count == 0)
            {
                return OnRecent(e);
            }

            string funcName = args[0];
            args = args.Skip(1).ToList();

            switch (funcName)
            {
                case "bind":
                    return OnBind(e, args);

                case "unbind":
                    return OnUnBind(e);

                case "recent":
                case "r":
                    return OnRecent(e);

                case "best":
                case "info":
                    return OnBest(e, args);

                case "b30":
                    return OnBest30(e, args);

                case "chart":
                    return OnChartPreview(e, args);

                case "song":
                    return OnSongInfo(e, args);

                default:
                    var chain = new RinMessageChain();
                    chain.Add(TextChain.Create($"[Arcaea]\n找不到功能: {funcName}"));
                    return chain;
            }
        }

        public RinMessageChain OnBest30(RinEvent e, List<string> args)
        {
            RinMessageChain chain = new RinMessageChain();
            ArcaeaBindInfo info = ArcaeaUserDB.Instance.GetBindInfo(e.SenderId, e.EventSourceType); ;
            string userCode;

            if (info != null) userCode = info.UserCode;
            else if (args.Count() > 0) userCode = args[0];
            else
            {
                chain.Add(TextChain.Create("[Arcaea]\n未查询到用户的绑定信息\n请先使用\n/arc bind <userCode/userName>\n进行绑定\n格式范例:\n/arc bind 114514810\n/arc bind YajuuSenpai"));
                return chain;
            }

            var result = ArcaeaUnlimitedAPI.Instance.GetBest30Result(info.UserCode).Result;
            if (result == null)
            {
                chain.Add(TextChain.Create("[Arcaea]\n查询时发生了错误\n远端服务器连接超时\n如果你是第一次查询 请几分钟后再重试"));
                return chain;
            }

            if (result.Status != 0)
            {
                chain.Add(TextChain.Create($"[Arcaea]\n查询时发生了错误\n{ArcaeaUnlimitedAPI.Instance.GetStatusTranslation(result.Status)}"));
                return chain;
            }
            var bytes = GraphGenerator.Instance.GenerateBest30(result);
            chain.Add(TextChain.Create("[Arcaea]Best30"));
            chain.Add(ImageChain.Create(bytes));
            return chain;
        }

        public RinMessageChain OnRecent(RinEvent e)
        {
            RinMessageChain chain = new RinMessageChain();
            ArcaeaBindInfo info = ArcaeaUserDB.Instance.GetBindInfo(e.SenderId, e.EventSourceType);

            if (info == null)
            {
                chain.Add(TextChain.Create("[Arcaea]\n未查询到用户的绑定信息\n请先使用\n/arc bind <userCode/userName>\n进行绑定\n格式范例:\n/arc bind 114514810\n/arc bind YajuuSenpai"));
                return chain;
            }

            var result = ArcaeaUnlimitedAPI.Instance.GetPlayerInfo(info.UserCode).Result;
            if (result == null)
            {
                chain.Add(TextChain.Create("[Arcaea]\n查询时发生了错误\n远端服务器连接超时\n如果你是第一次查询 请几分钟后再重试"));
                return chain;
            }

            if (result.Status != 0)
            {
                chain.Add(TextChain.Create($"[Arcaea]\n查询时发生了错误\n{ArcaeaUnlimitedAPI.Instance.GetStatusTranslation(result.Status)}"));
                return chain;
            }

            var bytes = GraphGenerator.Instance.GeneratePlayerResult(result);
            chain.Add(TextChain.Create("[Arcaea]Recent"));
            chain.Add(ImageChain.Create(bytes));
            return chain;
        }

        public RinMessageChain OnBest(RinEvent e, List<string> args)
        {
            RinMessageChain chain = new RinMessageChain();
            ArcaeaBindInfo info = ArcaeaUserDB.Instance.GetBindInfo(e.SenderId, e.EventSourceType);

            if (info == null)
            {
                chain.Add(TextChain.Create("[Arcaea]\n未查询到用户的绑定信息\n请先使用\n/arc bind <userCode/userName>\n进行绑定\n格式范例:\n/arc bind 114514810\n/arc bind YajuuSenpai"));
                return chain;
            }

            SongResult.SongDifficulty difficulty = SongResult.SongDifficulty.Future;
            if (args.Count() <= 0)
            {
                chain.Add(TextChain.Create($"[Arcaea]\n缺少参数 <songName>"));
                return chain;
            }
            if (args.Count() > 1)
            {
                var difficultyStr = args.Last();
                switch (difficultyStr.ToLower())
                {
                    case "0":
                    case "past":
                    case "pst":
                        difficulty = SongResult.SongDifficulty.Past;
                        args.RemoveAt(args.Count() - 1);
                        break;

                    case "1":
                    case "present":
                    case "prs":
                        difficulty = SongResult.SongDifficulty.Present;
                        args.RemoveAt(args.Count() - 1);
                        break;

                    case "2":
                    case "future":
                    case "ftr":
                        difficulty = SongResult.SongDifficulty.Future;
                        args.RemoveAt(args.Count() - 1);
                        break;

                    case "3":
                    case "beyond":
                    case "byd":
                    case "byn":
                        difficulty = SongResult.SongDifficulty.Beyond;
                        args.RemoveAt(args.Count() - 1);
                        break;
                }
            }
            string songName = String.Join(' ', args);

            var songList = ArcaeaSongDB.Instance.TryGetSong(songName);
            if (songList.Count <= 0)
            {
                chain.Add(TextChain.Create($"[Arcaea]\n未找到歌曲\n{songName}"));
                return chain;
            }
            var sample = songList.First();
            if (!songList.All(x => x.SongId == sample.SongId))
            {
                int index = 0;
                while (index < songList.Count - 1)
                {
                    if (songList[index + 1].SongId == songList[index].SongId)
                    {
                        songList.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }
                chain.Add(TextChain.Create($"[Arcaea]\n{songName} 是多项查询结果中的不确定关键字 请尝试补全关键字\n{(index > 3 ? String.Join('\n', songList.Take(3).Select(x => x.NameEN)) : String.Join('\n', songList.Select(x => x.NameEN)))}\n等 {index + 1} 个结果"));
                return chain;
            }

            var result = ArcaeaUnlimitedAPI.Instance.GetBestResult(info.UserCode, sample.SongId, difficulty).Result;
            if (result == null)
            {
                chain.Add(TextChain.Create($"[Arcaea]\n未找到结果\n{sample.NameEN} {difficulty}"));
                return chain;
            }
            if (result.Status != 0)
            {
                chain.Add(TextChain.Create($"[Arcaea]\n查询时发生了错误\n{ArcaeaUnlimitedAPI.Instance.GetStatusTranslation(result.Status)}"));
                return chain;
            }

            var bytes = GraphGenerator.Instance.GeneratePlayerResult(result);
            chain.Add(TextChain.Create("[Arcaea]Best"));
            chain.Add(ImageChain.Create(bytes));
            return chain;
        }

        public RinMessageChain OnUnBind(RinEvent e)
        {
            RinMessageChain chain = new RinMessageChain();
            ArcaeaBindInfo bindInfo = ArcaeaUserDB.Instance.GetBindInfo(e.SenderId, e.EventSourceType);
            if (bindInfo == null)
            {
                chain.Add(TextChain.Create($"[Arcaea]\n未存在绑定信息"));
                return chain;
            }
            ArcaeaUserDB.Instance.DeleteBindInfo(bindInfo.UserId);
            chain.Add(TextChain.Create($"[Arcaea]\n已解除绑定"));
            return chain;
        }

        public RinMessageChain OnBind(RinEvent e, List<string> args)
        {
            RinMessageChain chain = new RinMessageChain();
            ArcaeaBindInfo bindInfo = ArcaeaUserDB.Instance.GetBindInfo(e.SenderId, e.EventSourceType);
            if (bindInfo != null)
            {
                ArcaeaPlayerInfo playerInfo = ArcaeaUserDB.Instance.GetPlayerInfo(bindInfo);
                chain.Add(TextChain.Create($"[Arcaea]\n已存在绑定信息 如需更换绑定 请先使用\n/arc unbind\n进行解绑\n{playerInfo.UserName}({playerInfo.UserCode})"));
                return chain;
            }

            string userCodeOrUserName;
            if (args.Count < 1)
            {
                chain.Add(TextChain.Create($"[Arcaea]\n缺少参数 <userCode/userName>"));
                return chain;
            }
            else
            {
                userCodeOrUserName = args[0];
            }

            var result = ArcaeaUnlimitedAPI.Instance.GetPlayerInfo(userCodeOrUserName).Result;
            if (result == null || result.Status != 0) result = ArcaeaUnlimitedAPI.Instance.GetPlayerInfoByName(userCodeOrUserName).Result;
            if (result == null)
            {
                chain.Add(TextChain.Create($"[Arcaea]\n服务器回报错误\n连接超时"));
                return chain;
            }

            if (result.Status != 0)
            {
                chain.Add(TextChain.Create($"[Arcaea]\n服务器回报错误\n({result.Status}){ArcaeaUnlimitedAPI.Instance.GetStatusTranslation(result.Status)}"));
                return chain;
            }
            var accountInfo = result.Content.AccountInfo;
            ArcaeaUserDB.Instance.UpdatePlayerInfo(accountInfo.UserCode, accountInfo.UserName);
            ArcaeaUserDB.Instance.UpdateBindInfo(e.SenderId, e.EventSourceType, accountInfo.UserCode);

            chain.Add(TextChain.Create($"[Arcaea]\n已成功绑定到用户\n{accountInfo.UserName}({accountInfo.UserCode})"));
            return chain;
        }

        public RinMessageChain OnChartPreview(RinEvent e, List<string> args)
        {
            RinMessageChain chain = new RinMessageChain();

            SongResult.SongDifficulty difficulty = SongResult.SongDifficulty.Future;
            if (args.Count() <= 0)
            {
                chain.Add(TextChain.Create($"[Arcaea]\n缺少参数 <songName>"));
                return chain;
            }
            if (args.Count() > 1)
            {
                var difficultyStr = args.Last();
                args.RemoveAt(args.Count() - 1);
                switch (difficultyStr.ToLower())
                {
                    case "0":
                    case "past":
                    case "pst":
                        difficulty = SongResult.SongDifficulty.Past;
                        args.RemoveAt(args.Count() - 1);
                        break;

                    case "1":
                    case "present":
                    case "prs":
                        difficulty = SongResult.SongDifficulty.Present;
                        args.RemoveAt(args.Count() - 1);
                        break;

                    case "2":
                    case "future":
                    case "ftr":
                        difficulty = SongResult.SongDifficulty.Future;
                        args.RemoveAt(args.Count() - 1);
                        break;

                    case "3":
                    case "beyond":
                    case "byd":
                    case "byn":
                        difficulty = SongResult.SongDifficulty.Beyond;
                        args.RemoveAt(args.Count() - 1);
                        break;
                }
            }
            string songName = String.Join(' ', args);

            var songList = ArcaeaSongDB.Instance.TryGetSong(songName);
            if (songList.Count <= 0)
            {
                chain.Add(TextChain.Create($"[Arcaea]\n未找到歌曲\n{songName}"));
                return chain;
            }
            var sample = songList.First();
            if (!songList.All(x => x.SongId == sample.SongId))
            {
                int index = 0;
                while (index < songList.Count - 1)
                {
                    if (songList[index + 1].SongId == songList[index].SongId)
                    {
                        songList.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }
                chain.Add(TextChain.Create($"[Arcaea]\n{songName} 是多项查询结果中的不确定关键字 请尝试补全关键字\n{(index > 3 ? String.Join('\n', songList.Take(3).Select(x => x.NameEN)) : String.Join('\n', songList.Select(x => x.NameEN)))}\n等 {index + 1} 个结果"));
                return chain;
            }

            if (songList.Count <= 3 && difficulty == SongResult.SongDifficulty.Beyond)
            {
                chain.Add(TextChain.Create($"[Arcaea]\n错误\n{sample.NameEN} 没有 Beyond 难度铺面"));
                return chain;
            }

            var bytes = ArcaeaUnlimitedAPI.Instance.GetChartPreview(sample.SongId, difficulty).Result;
            if (bytes == null)
            {
                chain.Add(TextChain.Create($"[Arcaea]\n服务器回报错误\n连接超时或目标铺面不存在"));
                return chain;
            }

            chain.Add(TextChain.Create("[Arcaea]Best"));
            chain.Add(ImageChain.Create(bytes));
            return chain;
        }

        public RinMessageChain OnSongInfo(RinEvent e, List<string> args)
        {
            RinMessageChain chain = new RinMessageChain();

            SongResult.SongDifficulty difficulty = SongResult.SongDifficulty.Future;
            if (args.Count() <= 0)
            {
                chain.Add(TextChain.Create($"[Arcaea]\n缺少参数 <songName>"));
                return chain;
            }

            string songName = String.Join(' ', args);

            var songList = ArcaeaSongDB.Instance.TryGetSong(songName);
            if (songList.Count <= 0)
            {
                chain.Add(TextChain.Create($"[Arcaea]\n未找到歌曲\n{songName}"));
                return chain;
            }
            var sample = songList.First();
            if (!songList.All(x => x.SongId == sample.SongId))
            {
                int index = 0;
                while (index < songList.Count - 1)
                {
                    if (songList[index + 1].SongId == songList[index].SongId)
                    {
                        songList.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }
                chain.Add(TextChain.Create($"[Arcaea]\n{songName} 是多项查询结果中的不确定关键字 请尝试补全关键字\n{(index > 3 ? String.Join('\n', songList.Take(3).Select(x => x.NameEN)) : String.Join('\n', songList.Select(x => x.NameEN)))}\n等 {index + 1} 个结果"));
                return chain;
            }

            songList.Sort((a, b) => a.RatingClass.CompareTo(b.RatingClass));
            var songId = songList.First().SongId;
            var nameEN = songList.First().NameEN;
            var nameJP = songList.First().NameJP;
            var bpm = songList.First().BPM;
            var pack = ArcaeaSongDB.Instance.GetPackName(songList.First().Set);
            var side = songList.First().Side == 0 ? "光芒侧" : "纷争侧";

            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine($"曲名: {nameEN}");
            if (nameJP != "")
                stringBuilder.AppendLine($"    {nameJP}");
            stringBuilder.AppendLine($"BPM: {bpm}");
            stringBuilder.AppendLine($"曲包: {pack} ({side})");
            stringBuilder.AppendLine($"  PST/PRS/FTR{(songList.Count > 3 ? "/BYD" : "")}");
            stringBuilder.AppendLine($"难度: {String.Join('/', songList.Select(x => x.GetDifficultyFriendly()).ToList())}");
            stringBuilder.AppendLine($"定数: {String.Join('/', songList.Select(x => (float)x.Rating / 10).ToList())}");
            stringBuilder.AppendLine($"物量: {String.Join('/', songList.Select(x => x.Note).ToList())}");


            chain.Add(TextChain.Create("[Arcaea]Song"));
            chain.Add(ImageChain.Create(GraphGenerator.Instance.GetCoverImg(sample.SongId).Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 80).ToArray()));
            chain.Add(TextChain.Create(stringBuilder.ToString()));
            
            return chain;
        }
    }
}
