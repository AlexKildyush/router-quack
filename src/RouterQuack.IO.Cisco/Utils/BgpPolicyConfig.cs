using System.Net;
using System.Text;
using RouterQuack.Core.Models;

namespace RouterQuack.IO.Cisco.Utils;

internal static class BgpPolicyConfig
{
    internal static void ApplyPolicyConfig(StringBuilder builder, int asNumber, IEnumerable<Interface> ebgpInterfaces)
    {
        AppendCommunityLists(builder, asNumber);

        foreach (var @interface in ebgpInterfaces)
        {
            if (@interface.Bgp == BgpRelationship.None)
                continue;

            if (@interface.Neighbour?.Ipv4Address?.IpAddress is { } neighbourV4)
                AppendRouteMaps(builder, asNumber, @interface.Bgp, neighbourV4);

            if (@interface.Neighbour?.Ipv6Address?.IpAddress is { } neighbourV6)
                AppendRouteMaps(builder, asNumber, @interface.Bgp, neighbourV6);
        }
    }

    internal static string GetInboundRouteMapName(BgpRelationship relationship, IPAddress neighbourAddress)
        => $"RM-IN-{relationship.ToString().ToUpperInvariant()}-{Sanitize(neighbourAddress)}";

    internal static string GetOutboundRouteMapName(BgpRelationship relationship, IPAddress neighbourAddress)
        => $"RM-OUT-{relationship.ToString().ToUpperInvariant()}-{Sanitize(neighbourAddress)}";

    private static string Sanitize(IPAddress address)
        => address.ToString().Replace('.', '_').Replace(':', '_');

    private static int GetLocalPreference(BgpRelationship relationship)
        => relationship switch
        {
            BgpRelationship.Client => 300,
            BgpRelationship.Peer => 200,
            BgpRelationship.Provider => 100,
            _ => throw new ArgumentOutOfRangeException(nameof(relationship), relationship, null)
        };

    private static int GetCommunitySuffix(BgpRelationship relationship)
        => relationship switch
        {
            BgpRelationship.Client => 10,
            BgpRelationship.Peer => 20,
            BgpRelationship.Provider => 30,
            _ => throw new ArgumentOutOfRangeException(nameof(relationship), relationship, null)
        };

    private static string GetCommunityListName(BgpRelationship relationship)
        => $"RQ-SRC-{relationship.ToString().ToUpperInvariant()}";

    private static string GetExportCommunityListName(BgpRelationship relationship)
        => $"RQ-EXPORT-{relationship.ToString().ToUpperInvariant()}";

    private static string GetCommunityValue(int asNumber, BgpRelationship relationship)
        => $"{asNumber}:{GetCommunitySuffix(relationship)}";

    private static void AppendCommunityLists(StringBuilder builder, int asNumber)
    {
        var supportedRelationships = new[] { BgpRelationship.Client, BgpRelationship.Peer, BgpRelationship.Provider };
        foreach (var relationship in supportedRelationships)
        {
            builder.AppendLine(
                $"ip community-list standard {GetCommunityListName(relationship)} permit {GetCommunityValue(asNumber, relationship)}");
        }

        builder.AppendLine(
            $"ip community-list standard {GetExportCommunityListName(BgpRelationship.Client)} permit {GetCommunityValue(asNumber, BgpRelationship.Client)}");
        builder.AppendLine(
            $"ip community-list standard {GetExportCommunityListName(BgpRelationship.Client)} permit {GetCommunityValue(asNumber, BgpRelationship.Peer)}");
        builder.AppendLine(
            $"ip community-list standard {GetExportCommunityListName(BgpRelationship.Client)} permit {GetCommunityValue(asNumber, BgpRelationship.Provider)}");

        builder.AppendLine(
            $"ip community-list standard {GetExportCommunityListName(BgpRelationship.Peer)} permit {GetCommunityValue(asNumber, BgpRelationship.Client)}");
        builder.AppendLine(
            $"ip community-list standard {GetExportCommunityListName(BgpRelationship.Peer)} permit {GetCommunityValue(asNumber, BgpRelationship.Peer)}");

        builder.AppendLine(
            $"ip community-list standard {GetExportCommunityListName(BgpRelationship.Provider)} permit {GetCommunityValue(asNumber, BgpRelationship.Client)}");
        builder.AppendLine("!");
    }

    private static void AppendRouteMaps(StringBuilder builder, int asNumber, BgpRelationship relationship, IPAddress neighbourAddress)
    {
        var inboundName = GetInboundRouteMapName(relationship, neighbourAddress);
        builder.AppendLine($"route-map {inboundName} permit 10");
        builder.AppendLine($" set local-preference {GetLocalPreference(relationship)}");
        builder.AppendLine($" set community {GetCommunityValue(asNumber, relationship)} additive");
        builder.AppendLine("!");

        var outboundName = GetOutboundRouteMapName(relationship, neighbourAddress);
        builder.AppendLine($"route-map {outboundName} permit 10");
        builder.AppendLine($" match community {GetExportCommunityListName(relationship)}");
        builder.AppendLine($"route-map {outboundName} deny 20");
        builder.AppendLine("!");
    }
}
