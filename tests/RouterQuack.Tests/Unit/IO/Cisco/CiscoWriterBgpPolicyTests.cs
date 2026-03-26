using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.IO.Cisco;
using RouterQuack.Tests.Unit.TestHelpers;
using System.Net;

namespace RouterQuack.Tests.Unit.IO.Cisco;

public class CiscoWriterBgpPolicyTests
{
    private readonly ILogger<CiscoWriter> _logger = Substitute.For<ILogger<CiscoWriter>>();

    [Test]
    public async Task WriteFiles_PeerAndProviderRelationships_EmitDifferentPolicies()
    {
        var peerConfig = GenerateConfig(BgpRelationship.Peer);
        var providerConfig = GenerateConfig(BgpRelationship.Provider);

        await Assert.That(peerConfig).Contains("set local-preference 200");
        await Assert.That(providerConfig).Contains("set local-preference 100");
        await Assert.That(peerConfig).Contains("match community RQ-EXPORT-PEER");
        await Assert.That(providerConfig).Contains("match community RQ-EXPORT-PROVIDER");
    }

    [Test]
    public async Task WriteFiles_ClientRelationship_ExportsAllRelationshipTags()
    {
        var config = GenerateConfig(BgpRelationship.Client);

        await Assert.That(config).Contains("ip community-list standard RQ-EXPORT-CLIENT permit 65000:10");
        await Assert.That(config).Contains("ip community-list standard RQ-EXPORT-CLIENT permit 65000:20");
        await Assert.That(config).Contains("ip community-list standard RQ-EXPORT-CLIENT permit 65000:30");
    }

    [Test]
    public async Task WriteFiles_PeerRelationship_BlocksProviderRoutesOnExport()
    {
        var config = GenerateConfig(BgpRelationship.Peer);

        await Assert.That(config).Contains("ip community-list standard RQ-EXPORT-PEER permit 65000:10");
        await Assert.That(config).Contains("ip community-list standard RQ-EXPORT-PEER permit 65000:20");
        await Assert.That(config).DoesNotContain("ip community-list standard RQ-EXPORT-PEER permit 65000:30");
    }

    [Test]
    public async Task WriteFiles_ProviderRelationship_ExportsOnlyClientRoutes()
    {
        var config = GenerateConfig(BgpRelationship.Provider);

        await Assert.That(config).Contains("ip community-list standard RQ-EXPORT-PROVIDER permit 65000:10");
        await Assert.That(config).DoesNotContain("ip community-list standard RQ-EXPORT-PROVIDER permit 65000:20");
        await Assert.That(config).DoesNotContain("ip community-list standard RQ-EXPORT-PROVIDER permit 65000:30");
    }

    [Test]
    public async Task WriteFiles_BgpNeighbourWithDualStack_AttachesPoliciesForIpv4AndIpv6()
    {
        var config = GenerateConfig(BgpRelationship.Peer);

        await Assert.That(config).Contains("neighbor 198.51.100.2 route-map RM-IN-PEER-198_51_100_2 in");
        await Assert.That(config).Contains("neighbor 198.51.100.2 route-map RM-OUT-PEER-198_51_100_2 out");
        await Assert.That(config).Contains("neighbor 2001:db8::2 route-map RM-IN-PEER-2001_db8__2 in");
        await Assert.That(config).Contains("neighbor 2001:db8::2 route-map RM-OUT-PEER-2001_db8__2 out");
    }

    [Test]
    public async Task WriteFiles_IbgpNeighbours_EnableSendCommunity()
    {
        var config = GenerateConfig(
            BgpRelationship.Peer,
            includeIbgpNeighbour: true);

        await Assert.That(config).Contains("neighbor 10.0.0.2 send-community");
    }

    private string GenerateConfig(BgpRelationship relationship, bool includeIbgpNeighbour = false)
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"router-quack-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var localInterface = TestData.CreateInterface(
                name: "GigabitEthernet1/0",
                bgp: relationship);

            var remoteInterface = TestData.CreateInterface(
                name: "GigabitEthernet1/0",
                bgp: GetRemoteRelationship(relationship));

            localInterface.Neighbour = remoteInterface;
            remoteInterface.Neighbour = localInterface;

            localInterface.Ipv4Address = TestData.CreateAddress("198.51.100.1", 31);
            localInterface.Ipv6Address = TestData.CreateAddress("2001:db8::1", 127);
            remoteInterface.Ipv4Address = TestData.CreateAddress("198.51.100.2", 31);
            remoteInterface.Ipv6Address = TestData.CreateAddress("2001:db8::2", 127);

            var localRouter = TestData.CreateRouter(
                name: "R1",
                id: IPAddress.Parse("1.1.1.1"),
                loopbackAddressV4: IPAddress.Parse("10.0.0.1"),
                interfaces: [localInterface],
                useDefaultId: false);

            var routers = new List<Router> { localRouter };

            if (includeIbgpNeighbour)
            {
                routers.Add(TestData.CreateRouter(
                    name: "R2",
                    id: IPAddress.Parse("2.2.2.2"),
                    loopbackAddressV4: IPAddress.Parse("10.0.0.2"),
                    interfaces: [],
                    useDefaultId: false));
            }

            var localAs = TestData.CreateAs(number: 65000, igp: IgpType.iBGP, routers: routers);
            var remoteRouter = TestData.CreateRouter(
                name: "EXT1",
                id: IPAddress.Parse("3.3.3.3"),
                external: true,
                interfaces: [remoteInterface],
                useDefaultId: false);
            var remoteAs = TestData.CreateAs(number: 65100, igp: IgpType.OSPF, routers: [remoteRouter]);

            var context = ContextFactory.Create(asses: [localAs, remoteAs]);
            var writer = new CiscoWriter(_logger, context);
            writer.WriteFiles(outputDirectory);

            var configPath = Path.Combine(outputDirectory, "65000", "R1.cfg");
            return File.ReadAllText(configPath);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, true);
        }
    }

    private static BgpRelationship GetRemoteRelationship(BgpRelationship relationship)
        => relationship switch
        {
            BgpRelationship.Client => BgpRelationship.Provider,
            BgpRelationship.Provider => BgpRelationship.Client,
            _ => relationship
        };
}
