﻿using System.Reactive.Linq;
using Konata.Core.Interfaces.Api;
using SuzuBot.Core.EventArgs.Bot;
using SuzuBot.Core.Modules;

namespace SuzuBot.Modules.Entertaining;
public class PokeModule : BaseModule
{
    IDisposable _listener;

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
        "━Σ(ﾟДﾟ|||)━\n*错愕*\n*惊慌失措 15s*",
        "Ja, Treffer!",
        "Ziel hat beschuss unbeschädigt überstanden."
    };

    public PokeModule()
    {
        Name = "戳一戳";
    }

    public override bool Enable()
    {
        _listener = Context.EventChannel
            .Where(x => x is PokeEventArgs)
            .Select(x => (PokeEventArgs)x)
            .Subscribe(x => PokeHandler(x));
        return base.Enable();
    }

    public override bool Disable()
    {
        _listener.Dispose();
        return base.Disable();
    }

    public Task PokeHandler(PokeEventArgs eventArgs)
    {
        if (eventArgs.PokeType == PokeType.Group && eventArgs.ReceiverId != eventArgs.Bot.Uin)
            return Task.CompletedTask;

        var random = new Random();
        if (random.Next(100) < 25)
        {
            if (eventArgs.PokeType == PokeType.Friend)
                return eventArgs.Bot.SendFriendPoke(eventArgs.SenderId);
            else
                return eventArgs.Bot.SendGroupPoke(eventArgs.SubjectId, eventArgs.SenderId);
        }
        else
        {
            string reply = _pokeReplys[random.Next(_pokeReplys.Length)];
            if (eventArgs.PokeType == PokeType.Friend)
                return eventArgs.Bot.SendFriendMessage(eventArgs.SenderId, reply);
            else
                return eventArgs.Bot.SendGroupMessage(eventArgs.SubjectId, reply);
        }
    }
}
