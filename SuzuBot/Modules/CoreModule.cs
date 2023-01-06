using Konata.Core;
using SuzuBot.Core.Attributes;
using SuzuBot.Core.EventArgs.Message;
using SuzuBot.Core.Modules;

namespace SuzuBot.Modules;

public class CoreModule : BaseModule
{
	public CoreModule()
	{
		Name = "核心模块";
	}

	[Command("Ping", "^ping$")]
	public Task Ping(Bot bot, MessageEventArgs eventArgs, string[] args)
	{
		return eventArgs.Reply("Pong");
	}
}
