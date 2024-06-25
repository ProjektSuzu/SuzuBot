using System.Diagnostics;
using Lagrange.Core.Message.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuzuBot.Database;
using SuzuBot.Hosting;
using SuzuBot.Services;
using Prefix = SuzuBot.Commands.Attributes.Prefix;

namespace SuzuBot.Extensions;

internal static class MiddlewareExtensions
{
    public static SuzuAppHost UseMetrics(this SuzuAppHost app)
    {
        return app.Use(
            (ctx, next) =>
            {
                var metrics = ctx.Services.GetRequiredService<BotMetrics>();
                metrics.MessageCount++;
                return next(ctx);
            }
        );
    }

    public static SuzuAppHost UseCache(this SuzuAppHost app)
    {
        return app.Use(
            (ctx, next) =>
            {
                var cache = ctx.Services.GetRequiredService<MessageCache>();
                cache.Add(ctx.Chain);
                return next(ctx);
            }
        );
    }

    public static SuzuAppHost UsePrefixCheck(this SuzuAppHost app)
    {
        return app.Use(
            (ctx, next) =>
            {
                if (ctx.Input.StartsWith("/"))
                {
                    ctx.Input = ctx.Input[1..];
                    ctx.MessagePrefix |= Prefix.Prefix;
                }
                if (ctx.Chain.OfType<MentionEntity>().FirstOrDefault()?.Uin == ctx.Bot.BotUin)
                    ctx.MessagePrefix |= Prefix.Mention;

                return next(ctx);
            }
        );
    }

    public static SuzuAppHost UseRoute(this SuzuAppHost app)
    {
        return app.Use(
            (ctx, next) =>
            {
                var commandManager = ctx.Services.GetRequiredService<CommandManager>();
                if (commandManager.Match(ctx))
                    return next(ctx);
                else
                    return Task.CompletedTask;
            }
        );
    }

    public static SuzuAppHost UseRuleCheck(this SuzuAppHost app)
    {
        return app.Use(
            (ctx, next) =>
            {
                if (ctx.MessagePrefix == 0 && ctx.CommandPrefix != 0)
                    return Task.CompletedTask;

                var dbCtx = ctx.Services.GetRequiredService<SuzuDbContext>();
                var groupRule = dbCtx.GroupRules.FirstOrDefault(x =>
                    x.GroupUin == ctx.Group.GroupUin && x.CommandId == ctx.Command!.Id
                );
                if (groupRule is not null)
                {
                    if (groupRule.Rule == "deny")
                        return Task.CompletedTask;
                }

                var userInfo = dbCtx.UserInfos.Find(ctx.Member.Uin);
                if (userInfo is null)
                {
                    userInfo = new()
                    {
                        Uin = ctx.Member.Uin,
                        Coin = 0,
                        Exp = 0,
                        Permission = Commands.Attributes.Permission.User,
                    };
                    dbCtx.UserInfos.Add(userInfo);
                    dbCtx.SaveChanges();
                }

                if (
                    userInfo.Permission < Commands.Attributes.Permission.Owner
                    && ctx.Member.Permission
                        >= Lagrange.Core.Common.Entity.GroupMemberPermission.Admin
                )
                    userInfo.Permission = Commands.Attributes.Permission.Admin;

                if (userInfo.Permission < ctx.Command!.RouteRule.Permission)
                    return Task.CompletedTask;

                return next(ctx);
            }
        );
    }

    public static SuzuAppHost UseInvoke(this SuzuAppHost app)
    {
        return app.Use(
            async (ctx, next) =>
            {
                if (ctx.Command is not null && ctx.ParseResult is not null)
                {
                    var logger = ctx.Services.GetRequiredService<ILogger<CommandManager>>();
                    logger.LogInformation("Invoking command {}", ctx.Command.Id);
                    var metrics = ctx.Services.GetRequiredService<BotMetrics>();
                    metrics.CommandCount++;
                    try
                    {
                        Stopwatch stopwatch = new();
                        stopwatch.Start();
                        await ctx.Command.ExecuteAsync(ctx);
                        stopwatch.Stop();
                        logger.LogInformation(
                            "Command {} completed in {}ms",
                            ctx.Command.Id,
                            stopwatch.ElapsedMilliseconds
                        );
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Command {} failed", ctx.Command.Id);
                    }
                }

                await next(ctx);
            }
        );
    }
}
