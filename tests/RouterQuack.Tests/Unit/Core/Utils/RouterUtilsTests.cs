using System.Net.Sockets;
using RouterQuack.Core.Utils;

namespace RouterQuack.Tests.Unit.Core.Utils;

public class RouterUtilsTests
{
    private readonly RouterUtils _utils = new();

    [Test]
    public async Task GetDefaultId_SameName_ReturnsSameId()
    {
        var id1 = _utils.GetDefaultId("Router1");
        var id2 = _utils.GetDefaultId("Router1");

        await Assert.That(id1).IsEqualTo(id2);
    }

    [Test]
    public async Task GetDefaultId_DifferentNames_ReturnsDifferentIds()
    {
        var id1 = _utils.GetDefaultId("Router1");
        var id2 = _utils.GetDefaultId("Router2");

        await Assert.That(id1).IsNotEqualTo(id2);
    }

    [Test]
    public async Task GetDefaultId_ReturnsValidIPv4()
    {
        var id = _utils.GetDefaultId("TestRouter");

        await Assert.That(id.AddressFamily).IsEqualTo(AddressFamily.InterNetwork);
        await Assert.That(id.GetAddressBytes()).Count().IsEqualTo(4);
    }
}