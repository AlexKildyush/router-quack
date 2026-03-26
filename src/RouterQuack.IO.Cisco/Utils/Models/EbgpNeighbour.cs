using System.Net;
using RouterQuack.Core.Models;

namespace RouterQuack.IO.Cisco.Utils.Models;

public readonly struct EbgpNeighbour
{
    public BgpRelationship Relationship { get; }

    public int RemoteAsNumber { get; }

    public IPAddress? AddressV4 { get; }

    public IPAddress? AddressV6 { get; }

    public EbgpNeighbour(Interface @interface)
    {
        Relationship = @interface.Bgp;
        RemoteAsNumber = @interface.Neighbour!.ParentRouter.ParentAs.Number;
        AddressV4 = @interface.Neighbour.Ipv4Address?.IpAddress;
        AddressV6 = @interface.Neighbour.Ipv6Address?.IpAddress;
    }
}