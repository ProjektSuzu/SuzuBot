using Konata.Core.Message;
using Konata.Core.Message.Model;
using SuzuBot.Core.Attributes;
using SuzuBot.Core.EventArgs.Message;
using SuzuBot.Core.Modules;

namespace SuzuBot.Modules.Entertaining;
public class MiniGameModule : BaseModule
{
    public MiniGameModule()
    {
        Name = "娱乐小游戏";
    }

    [Command("抽老婆", "^roll_waifu$", "^抽老婆$", "^clp$")]
    public async Task RollMemberAsWaifu(GroupMessageEventArgs eventArgs, string[] args)
    {
        var seed = int.Parse(DateTime.Today.ToString("yyyyMMdd")) + eventArgs.Sender.Id;
        var rnd = new Random((int)seed);

        var members = eventArgs.Group.Value.Members.OrderBy(x => x.Id).ToList();
    Roll:
        var member = members[rnd.Next(members.Count - 1)];
        if (member.Id == eventArgs.Sender.Id)
            goto Roll;
        MessageBuilder builder = new("今天你的群友老婆是\n")
        {
            ImageChain.CreateFromUrl(member.AvatarUrl),
            TextChain.Create($"{member.Name}({member.Id})")
        };
        await eventArgs.Reply(builder);
    }
}
