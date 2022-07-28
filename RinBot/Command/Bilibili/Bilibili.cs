using RestSharp;
using RinBot.Core.Component.Command.CustomAttribute;
using RinBot.Core.Component.Event;
using RinBot.Core.Component.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Command.Bilibili
{
    [Module("BiliBili", "org.akulak.bilibili")]
    internal class Bilibili
    {
        [Command("查成分", new[] { "ccf", "chachengfeng" , "查成分" }, (int)MatchingType.StartsWith, ReplyType.Reply)]
        public RinMessageChain OnIngredientCheck(RinEvent e, List<string> args)
        {
            var chains = new RinMessageChain();
            chains.Add("[BiliBili]\n");
            if (args.Count <= 0)
            {
                chains.Add("未指定要查询的用户");
                return chains;
            }
            var target = args.First();
            var result = int.TryParse(target, out _) ? IngredientCheck.Instance.Check(int.Parse(target)) : IngredientCheck.Instance.Check(target);
            if (result.Ingredients == null)
            {
                chains.Add("目标用户不存在或者关注不可见");
                return chains;
            }
            chains.Add($"{result.UserName} 关注了 {result.Ingredients.Count} 个Vtb:\n");
            chains.Add(String.Join('、', result.Ingredients.Select(x => x.UserName)));
            return chains;
        }
    }
}
