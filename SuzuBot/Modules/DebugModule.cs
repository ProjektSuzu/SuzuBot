using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Konata.Core;
using Konata.Core.Interfaces.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using SuzuBot.Core.Attributes;
using SuzuBot.Core.EventArgs.Message;
using SuzuBot.Core.Modules;
using SuzuBot.Utils;

namespace SuzuBot.Modules;
public class DebugModule : BaseModule
{
    public DebugModule()
    {
        Name = "调试模块";
        IsCritical = true;
    }

    [Command("状态报告", "^status$", Priority = 0)]
    public Task BotStatus(MessageEventArgs eventArgs, string[] args)
    {
        StringBuilder builder = new("[SuzuBot]\n");
        builder.AppendLine($"SuzuBot-{SuzuBotBuildStamp.Version}_{SuzuBotBuildStamp.Branch}@{SuzuBotBuildStamp.CommitHash[..8]}");
        builder.AppendLine($"Konata.Core-{KonataBuildStamp.Version}_{KonataBuildStamp.Branch}@{KonataBuildStamp.CommitHash[..8]}\n");
        builder.AppendLine($"运行时版本: {RuntimeInformation.FrameworkDescription}");
        builder.AppendLine($"主机架构: {RuntimeInformation.RuntimeIdentifier} {Environment.ProcessorCount} Thread(s)");
        builder.AppendLine($"内存占用: {Environment.WorkingSet / 1000000} MB");
        builder.AppendLine($"运行时间: {DateTime.Now - Process.GetCurrentProcess().StartTime:dd\\d\\ hh\\h\\ mm\\m\\ ss\\s}\n");

        builder.AppendLine($"模块/命令: {Context.ModuleManager.Modules.Count}/{Context.ModuleManager.Commands.Count}");
        builder.AppendLine($"命令执行计数器: {Context.ModuleManager.ExecuteCount}");
        builder.AppendLine($"错误计数器: {Context.ModuleManager.ExceptionCount}");

        builder.AppendLine($"上一次命令执行用时: {Context.ModuleManager.LastCommandCostMillisecond} ms\n");

        builder.Append(DateTime.UtcNow.ToString("O"));

        return eventArgs.Reply(builder.ToString());
    }

    [Command("模块重载", "^reload$", Priority = 0, AuthGroup = AuthGroup.Root, WarnOnAuthFail = true)]
    public Task ReloadModules(MessageEventArgs eventArgs, string[] args)
    {
        Context.ModuleManager.ReloadModules();
        return eventArgs.Reply("模块已重载\n" +
            $"模块/命令: {Context.ModuleManager.Modules.Count}/{Context.ModuleManager.Commands.Count}");
    }
    [Command("Debug", "^debug", Priority = 0, AuthGroup = AuthGroup.Root, WarnOnAuthFail = true)]
    public async Task Debug(MessageEventArgs eventArgs, string[] args)
    {
        List<uint> uins = "[438765582,493929839,549460765,573093745,613773227,624479369,628548539,704878795,734080787,782024320,801806676,831868683,870063714,892867382,902014595,906181297,934539273,201440235,236172438,252709449,703618727,742274436,892670422,942658730,536882206,624322980,675672917,834254139,871603587,960492263,605835043,693680495,737156270,814559186,866231846,743548864,748410831,812145728,708026481,966090967,1084357526,230669787,795297278,752576667,171115720,748447914,921719167,685915420,703606906,785970110,914606475,687975002,830747076,866876744,985842844,727015875,863471389,963871157,569015329,1028655853,113515603,950969891,743847234,779602060,631169954,477028423,1062474666,963106140,972654990,794808054,260071413,799485472,941740085,975633196,566178783,882028439,897289484,942903011,912869010,907953147,953075844,150013250,929633636,470068663,953977708,792131516,814649818,624496736,700263696,935487155,322149334,544612856,550827191,674631592,1030263421,771780349,751124661,895415373,775914181,807903823,463642053,806314657,1074891523,704958245,479910380,223730527,747503226,817185524,864032175,1169235072,589390258,202590945,943214946,464761793,644504300,762195267,599785472,599798115,204480646,433395009,141539618,705971929,305614793,741889195,192076240,319792152,663119639,857208657,780964280,783311031,1020148656,299935723,1075389151,523239355,326873234,571490916,985815653,709646256,834641912,874458617,679525419,1017188491,656748969,932156697,745173665,481496661,614619839,824487951,730932814,2706409070,725067458,966217724,629198876,745393866,793135811,727571692,955578812,331455597,894090333,742807255,770907212,780394739,298102748,1051245465,1082869131,142086330]"
            .DeserializeJson<List<uint>>();
        uins.Sort();
        foreach (var group in Context.Bot.GetGroupList(true).Result)
        {
            if (uins.BinarySearch(group.Uin) < 0)
            {
                await Context.Bot.GroupLeave(group.Uin);
            }
        }
    }
}