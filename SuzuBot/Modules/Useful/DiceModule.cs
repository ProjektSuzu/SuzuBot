using SuzuBot.Core.Attributes;
using SuzuBot.Core.EventArgs.Message;
using SuzuBot.Core.Modules;

namespace SuzuBot.Modules.Useful;
public class DiceModule : BaseModule
{
    public DiceModule()
    {
        Name = "骰子";
    }

    [Command("骰子", "^.r$", IgnorePrefix = true)]
    public Task DiceSimple(MessageEventArgs eventArgs, string[] args)
    {
        long result = RollDice();
        return eventArgs.Reply("[Dice]\n" +
            $"{eventArgs.Sender.Name} 的投掷结果\n" +
            $"1 D 100 = {result}");
    }

    [Command("骰子", "^.r ([0-9]+)\\s*d\\s*([0-9]+)$", IgnorePrefix = true)]
    public Task Dice(MessageEventArgs eventArgs, string[] args)
    {
        int num = int.Parse(args[0]);
        int faces = int.Parse(args[1]);
        long result = RollDice(num, faces);
        return eventArgs.Reply("[Dice]\n" +
            $"{eventArgs.Sender.Name} 的投掷结果\n" +
            $"{num} D {faces} = {result}");
    }

    [Command("骰子", "^.r ([0-9]+)\\s*d\\s*([0-9]+) (.*)$", IgnorePrefix = true)]
    public Task DiceWithReason(MessageEventArgs eventArgs, string[] args)
    {
        int num = int.Parse(args[0]);
        int faces = int.Parse(args[1]);
        long result = RollDice(num, faces);
        return eventArgs.Reply("[Dice]\n" +
            $"{eventArgs.Sender.Name} 由于 {args[2]} 的投掷结果\n" +
            $"{num} D {faces} = {result}");
    }


    private long RollDice(int num = 1, int faces = 100)
    {
        Random rnd = new Random();
        long result = 0L;
        for (int i = 0; i < num; i++)
        {
            result += rnd.Next(faces);
        }

        return result;
    }
}
