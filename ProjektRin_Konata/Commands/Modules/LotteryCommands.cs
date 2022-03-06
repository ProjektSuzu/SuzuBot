using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using ProjektRin.Utils.Database.Tables;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("抽奖")]
    internal class LotteryCommands : BaseCommand
    {
        public static string help =
                $"[抽奖]\n" +
                $"/roll [<num>]      玩 num 次老虎机\n" +
                $"                   默认玩一次\n" +
                $"/<num>连           玩 num 次老虎机\n" +
                $"\n" +
                $"快捷名:\n" +
                $"/抽奖\n" +
                $"\n" +
                $"  num     游玩次数";

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
            "♠",
            "♥",
            "♣",
            "♦",
            "★",
            "★",
            "⑦"
        };

        public override void OnInit()
        {
        }

        [GroupMessageCommand("老虎机", new[] { @"^roll\s?([0-9]+)?", @"^抽奖\s?([0-9]+)?", @"([0-9]+)连" })]
        public void OnRoll(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var reply = "";
            var uin = messageEvent.MemberUin;
            var ticket = 500u;
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
                    $"抽奖需要 {CoinToString(ticket)}, 而你只有 {CoinToString(info.coin)}.";
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
                                    coin += 10000;
                                    break;
                                }

                            case "⑦":
                                {
                                    reward = "头奖";
                                    coin += 50000;
                                    break;
                                }
                        }
                    }
                    else if (result.TakeWhile(x => x == "★").Count() == 2)
                    {
                        reward = "双星";
                        coin += 5000;
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
                        $"内存+ {CoinToString(coin)}";

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
                        $"{times} 抽完成, 总计: 内存+ {CoinToString(totalCoin)}";
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
                                coin += 10000;
                                break;
                            }

                        case "⑦":
                            {
                                reward = "头奖";
                                coin += 50000;
                                break;
                            }
                    }
                }
                else if (result.TakeWhile(x => x == "★").Count() == 2)
                {
                    reward = "双星";
                    coin += 5000;
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
                    $"内存+ {CoinToString(coin)}";

                info.coin += coin;
                UserInfoManager.UpdateUserInfo(info);
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }
            
            
        }

        public static string CoinToString(uint coin)
        {
            if (coin < 1000)
            {
                return $"{coin} Byte";
            }
            else if (coin < 1000000)
            {
                return $"{(float)coin / 1000:f3} KB";
            }
            else if (coin < 1000000000)
            {
                return $"{(float)coin / 1000000:f3} MB";
            }
            else
            {
                return $"{(float)coin / 1000000000:f3} GB";
            }
        }

        private string RollOnce()
        {
            return wheel[new Random().Next(wheel.Length)];
        }

        
    }
}
