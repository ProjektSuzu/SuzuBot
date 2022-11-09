using System.Reflection;
using Konata.Core;

#pragma warning disable CS8602

namespace SuzuBot.Utils;

public static class KonataBuildStamp
{
    public static string Branch
        => Stamp[0];

    public static string CommitHash
        => Stamp[1][..16];

    public static string BuildTime
        => Stamp[2];

    public static string Version
        => InformationalVersion;

    private static readonly string[] Stamp
        = typeof(Bot).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(x => x.Key == "BuildStamp").Value.Split(";");

    private static readonly string InformationalVersion
        = typeof(Bot).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
}
