using System.Reflection;
using Konata.Core;
using Konata.Core.Interfaces.Api;
using SuzuBot.Common;
using SuzuBot.Common.Attributes;

namespace SuzuBot.Modules;

[Module("戳一戳")]
internal class PokeModule : BaseModule
{
    private static string[] _pokeReplys = new string[]
    {
        "别戳了(*>∀＜*)很痒的呀",
        "｡ﾟ･ (｡>﹏<) ･ﾟ｡再戳就要戳坏了",
        "铃才不是你的电子宠物(//>︿<)",
        "再戳我下次就改你签(￣ヘ￣)",
        "呜呜(//> △ <)不理你了",
        "正在启动自毁程序",
        "我得重新集结部队",
        "为时已晚，有机体",
        "Ja, Treffer!",
    };

    public override bool Enable()
    {
        Context.Bot.OnGroupPoke += Bot_OnGroupPoke;
        Context.Bot.OnFriendPoke += Bot_OnFriendPoke;
        return base.Enable();
    }

    public override void Disable()
    {
        Context.Bot.OnGroupPoke -= Bot_OnGroupPoke;
        Context.Bot.OnFriendPoke -= Bot_OnFriendPoke;
        base.Disable();
    }

    private void Bot_OnFriendPoke(Bot sender, Konata.Core.Events.Model.FriendPokeEvent args)
    {
        if (args.FriendUin == sender.Uin) return;
        _ = PokeHandler(sender, args.FriendUin, 0U);
    }

    private void Bot_OnGroupPoke(Bot sender, Konata.Core.Events.Model.GroupPokeEvent args)
    {
        if (args.MemberUin != sender.Uin) return;
        _ = PokeHandler(sender, args.OperatorUin, args.GroupUin);
    }

    public async Task PokeHandler(Bot bot, uint senderUin, uint groupUin)
    {
        var random = new Random();
        if (random.Next(100) < 30)
        {
            if (groupUin == 0U)
                await bot.SendFriendPoke(senderUin);
            else
                await bot.SendGroupPoke(groupUin, senderUin);
        }
        else
        {
            string reply = _pokeReplys[random.Next(_pokeReplys.Length)];
            if (groupUin == 0U)
                await bot.SendFriendMessage(senderUin, reply);
            else
                await bot.SendGroupMessage(groupUin, reply);
        }
    }
}
