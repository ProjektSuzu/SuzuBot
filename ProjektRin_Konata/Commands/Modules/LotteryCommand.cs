using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using Newtonsoft.Json;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using ProjektRin.Components;
using ProjektRin.Utils.Database.Tables;
using System.Text;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("抽奖", "com.akulak.lottery")]
    internal class LotteryCommand : BaseCommand
    {
        public override string Help =>
                $"[抽奖]\n" +
                $"/slot [<num>]      玩 num 次抽奖机\n" +
                $"                   默认玩一次\n" +
                $"/<num>连           玩 num 次抽奖机\n" +
                $"\n" +
                $"  num     游玩次数" +
                $"\n" +
                $"快捷名:\n" +
                $"/抽奖\n" +
                $"\n" +
                $"/lottery [<number>]   购买和查看大乐透\n" +
                $"\n" +
                $"  number  想要购买的号码 只能选择3个 0-9 的不同的数字\n" +
                $"\n" +
                $"快捷名:\n" +
                $"/大乐透";

        private static readonly string[] wheel = new[]
        {
            "♠",
            "♥",
            "♣",
            "♦",
            "♠",
            "♥",
            "♣",
            "♦",
            "♠",
            "♥",
            "♣",
            "♦",
            "★",
            "★",
            "⑦"
        };

        private static readonly List<int> lotteryNumber = new()
        {
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9,
        };

        private LotteryData data;

        private System.Timers.Timer timer;

        public override void OnInit()
        {
            Load();
            Save();
            SetTimer();
        }

        public void Save()
        {
            var json = JsonConvert.SerializeObject(data);
            File.WriteAllText(BotManager.resourcePath + "/lottery.json", json, Encoding.UTF8);
        }

        public void Load()
        {
            try
            {
                var json = File.ReadAllText(BotManager.resourcePath + "/lottery.json");
                var load = JsonConvert.DeserializeObject<LotteryData>(json);
                if (load == null)
                {
                    data = new LotteryData()
                    { Lotteries = new(), PrizePool = 10000, Winners = new(), WinningNumber = new() };
                }
                else
                {
                    data = load;
                }
            }
            catch
            {
                data = new LotteryData()
                { Lotteries = new(), PrizePool = 10000, Winners = new(), WinningNumber = new() };
            }


        }

        private void SetTimer()
        {
            timer = new System.Timers.Timer();
            var time1 = DateTime.Now;
            var time2 = DateTime.Now.AddDays(1).Date;

            int sec = (int)time2.Subtract(time1).TotalSeconds + 1;
            timer.AutoReset = false;
            timer.Interval = sec * 1000;
            timer.Elapsed += (sender, message) => OnDraw();
            timer.Start();
        }

        [GroupMessageCommand("抽奖", new[] { @"^slot\s?([0-9]+)?", @"^抽奖\s?([0-9]+)?", @"^抽奖([0-9]+)连" })]
        public void OnSlot(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var reply = "";
            var uin = messageEvent.MemberUin;
            var ticket = 1000u;
            var times = 1u;

            Console.WriteLine(times);

            if (args.Count > 0)
            {
                uint.TryParse(args[0], out times);
            }



            if (times <= 0) return;
            if (times > 100)
            {
                reply =
                    $"最多只能100连抽哦\n";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            ticket *= times;

            var reward = "";

            var info = UserInfoManager.GetUserInfo(uin);

            if (info.coin < ticket)
            {
                reply =
                    $"你的内存不足\n" +
                    $"抽奖需要 {UserInfoManager.CoinToString(ticket)}, 而你只有 {UserInfoManager.CoinToString(info.coin)}.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            info.coin -= ticket;

            if (times > 1)
            {
                var totalCoin = 0u;
                var multiReply = MultiMsgChain.Create();

                for (int i = 0; i < times; i++)
                {
                    var coin = 0u;
                    var result = new string[3];
                    for (int j = 0; j < 3; j++)
                    {
                        result[j] = RollOnce();
                    }

                    var a1 = result[0];

                    if (result.All(x => x == a1))
                    {
                        switch (a1)
                        {
                            case "♠":
                            case "♣":
                            case "♥":
                            case "♦":
                                {
                                    reward = "同花";
                                    coin += 1000;
                                    break;
                                }

                            case "★":
                                {
                                    reward = "三星";
                                    coin += 50000;
                                    break;
                                }

                            case "⑦":
                                {
                                    reward = "头奖";
                                    coin += 1000000;
                                    break;
                                }
                        }
                    }
                    else if (result.Count(x => x == "★") == 2)
                    {
                        reward = "双星";
                        coin += 10000;
                    }
                    else if (result.All(x => x == "♠" || x == "♣") || result.All(x => x == "♥" || x == "♦"))
                    {
                        reward = "同色";
                        coin += 500;
                    }
                    else
                    {
                        reward = "未中奖";
                    }

                    reply =
                        $"机会的转轮停下...结果是:\n" +
                        $"┌─┬─┬─┐\n" +
                        $"│{result[0]}│{result[1]}│{result[2]}│\n" +
                        $"└─┴─┴─┘\n" +
                        $"{reward}\n" +
                        $"内存+ {UserInfoManager.CoinToString(coin)}";

                    multiReply.AddMessage(
                        new SourceInfo(bot.Uin, bot.Name),
                        new MessageBuilder(reply)
                        );
                    totalCoin += coin;
                }
                info.coin += totalCoin;
                UserInfoManager.UpdateUserInfo(info);

                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(multiReply));
                reply =
                        $"{times} 抽完成, 总计: 内存+ {UserInfoManager.CoinToString(totalCoin)}";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }
            else
            {
                var coin = 0u;
                var result = new string[3];
                for (int j = 0; j < 3; j++)
                {
                    result[j] = RollOnce();
                }

                var a1 = result[0];
                if (result.All(x => x == a1))
                {
                    switch (a1)
                    {
                        case "♠":
                        case "♣":
                        case "♥":
                        case "♦":
                            {
                                reward = "同花";
                                coin += 1000;
                                break;
                            }

                        case "★":
                            {
                                reward = "三星";
                                coin += 50000;
                                break;
                            }

                        case "⑦":
                            {
                                reward = "头奖";
                                coin += 1000000;
                                break;
                            }
                    }
                }
                else if (result.Count(x => x == "★") == 2)
                {
                    reward = "双星";
                    coin += 10000;
                }
                else if (result.All(x => x == "♠" || x == "♣") || result.All(x => x == "♥" || x == "♦"))
                {
                    reward = "同色";
                    coin += 500;
                }
                else
                {
                    reward = "未中奖";
                }

                reply =
                    $"机会的转轮停下...结果是:\n" +
                    $"┌─┬─┬─┐\n" +
                    $"│{result[0]}│{result[1]}│{result[2]}│\n" +
                    $"└─┴─┴─┘\n" +
                    $"{reward}\n" +
                    $"内存+ {UserInfoManager.CoinToString(coin)}";

                info.coin += coin;
                UserInfoManager.UpdateUserInfo(info);
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }
        }

        [GroupMessageCommand("阿塔大乐透", new[] { @"^lottery\s?([0-9]+)?", @"^大乐透\s?([0-9]+)?" })]
        public void OnLottery(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var reply = "";
            if (args.Count <= 0)
            {
                reply =
                    $"上一期阿塔大乐透的开奖号码为: {String.Join('|', data.WinningNumber)}\n" +
                    $"当前奖池累计: {UserInfoManager.CoinToString(data.PrizePool)}\n" +
                    $"上一期中奖人数: {data.Winners.Count} 人\n" +
                    $"{(data.Winners.Contains(messageEvent.MemberUin) ? "恭喜你成为中奖人之一 奖金已经自动打入账户中" : "很遗憾 你并不是中奖人之一")}\n" +
                    $"如想购买大乐透 请使用 /lottery [<number>] 一次性输入3个不同的号码即可\n\n" +
                    $"本奖金由 阿塔w_Official 全额赞助";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }
            else
            {
                if (data.Lotteries.Any(x => x.Uin == messageEvent.MemberUin))
                {
                    var l = data.Lotteries.First(x => x.Uin == messageEvent.MemberUin);
                    reply = "你已经购买过彩票了 不要贪心哦\n" +
                        $"{String.Join('|', l.Number)}";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
                var number = args[0];
                if (number.Length != 3)
                {
                    reply = "输入非法: 只能选择3个 0-9 的不同的数字";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
                int a, b, c;

                if (!int.TryParse(number[0].ToString(), out a) ||
                    !int.TryParse(number[1].ToString(), out b) ||
                    !int.TryParse(number[2].ToString(), out c))
                {
                    reply = "输入非法: 只能选择3个 0-9 的不同的数字";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }

                if (a == b || a == c || b == c)
                {
                    reply = "输入非法: 只能选择3个 0-9 的不同的数字";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }

                if (a < 0 || a > 9 || b < 0 || b > 9 || c < 0 || c > 9)
                {
                    reply = "输入非法: 只能选择3个 0-9 的不同的数字";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }

                uint ticket = 1000;
                var info = UserInfoManager.GetUserInfo(messageEvent.MemberUin);
                if (info.coin < ticket)
                {
                    reply =
                    $"你的内存不足\n" +
                    $"抽奖需要 {UserInfoManager.CoinToString(ticket)}, 而你只有 {UserInfoManager.CoinToString(info.coin)}.";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }

                info.coin -= ticket;
                data.PrizePool += ticket;
                UserInfoManager.UpdateUserInfo(info);

                var numbers = new List<int>() { a, b, c };
                numbers.Sort();
                var lottery = new Lottery(messageEvent.MemberUin, numbers);
                data.Lotteries.Add(lottery);
                Save();
                reply =
                    $"购买成功\n" +
                    $"{String.Join('|', numbers)}\n" +
                    $"开奖时间为 {DateTime.Now.AddDays(1).Date:g} 祝您好运.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }
        }

        [GroupMessageCommand("彩票立即开奖", new[] { @"^draw_now" }, PermissionManager.Permission.Root)]
        public void OnDrawNow(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            OnDraw();
        }

        public void OnDraw()
        {
            timer.Dispose();
            var rnd = new Random();
            data.WinningNumber.Clear();
            data.WinningNumber = lotteryNumber.OrderBy(x => rnd.Next()).Take(3).ToList();
            data.WinningNumber.Sort();
            data.Winners.Clear();
            foreach (var l in data.Lotteries)
            {
                if (l.Number.All(data.WinningNumber.Contains))
                {
                    data.Winners.Add(l.Uin);
                }
            }
            data.Lotteries.Clear();

            if (data.Winners.Count > 0)
            {
                var prize = data.PrizePool / data.Winners.Count;
                data.PrizePool = 10000;
                foreach (var i in data.Winners)
                {
                    var info = UserInfoManager.GetUserInfo(i);
                    info.coin += (uint)prize;
                    UserInfoManager.UpdateUserInfo(info);
                }
            }
            Save();
            SetTimer();
        }

        private static string RollOnce()
        {
            return wheel[new Random().Next(wheel.Length)];
        }
    }

    class LotteryData
    {
        public uint PrizePool;
        public List<int> WinningNumber;
        public List<uint> Winners;
        public List<Lottery> Lotteries;
    }

    class Lottery
    {
        public uint Uin;
        public List<int> Number;

        public Lottery(uint uin, List<int> number)
        {
            this.Uin = uin;
            this.Number = number;
        }
    }
}
