using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace RouterQuack.Core.Utils;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
public sealed class RouterUtils
{
    /// <summary>
    /// Deterministically generates a unique router ID based on the provided <paramref name="routerName" />.
    /// </summary>
    /// <param name="routerName">The unique string name of the router.</param>
    /// <returns>A deterministic <see cref="IPAddress" /> representing the router's ID.</returns>
    [Pure]
    public IPAddress GetDefaultId(string routerName)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(routerName));

        var bytes = new byte[4];
        Array.Copy(hash, bytes, 4);

        bytes[0] = Math.Max((byte)1, bytes[0]);
        bytes[3] = Math.Max((byte)1, bytes[3]);

        return new(bytes);
    }
}