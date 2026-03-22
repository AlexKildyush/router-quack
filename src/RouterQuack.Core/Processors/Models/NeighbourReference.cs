namespace RouterQuack.Core.Processors.Models;

/// <summary>
/// Parsed reference to a neighbour router, with an optional neighbour interface name.
/// </summary>
public readonly struct NeighbourReference
{
    public int AsNumber { get; }

    public string RouterName { get; }

    public string? InterfaceName { get; }

    /// <summary>
    /// Parse neighbour reference from path.
    /// </summary>
    /// <param name="neighbourPath">Path in `router[:interface]` or `as:router[:interface]` form.</param>
    /// <param name="interface">The current interface.</param>
    /// <returns>Parsed <see cref="NeighbourReference"/>; invalid values produce an unresolvable reference.</returns>
    public NeighbourReference(string? neighbourPath, Interface @interface)
    {
        var segments = neighbourPath?.Split(':') ?? [];

        (AsNumber, RouterName, InterfaceName) = segments.Length switch
        {
            1 => (@interface.ParentRouter.ParentAs.Number, segments[0], null),
            2 when int.TryParse(segments[0], out var asNumber) => (asNumber, segments[1], null),
            2 => (@interface.ParentRouter.ParentAs.Number, segments[0], segments[1]),
            3 when int.TryParse(segments[0], out var asNumber) => (asNumber, segments[1], segments[2]),
            _ => (0, string.Empty, null)
        };
    }

    /// <summary>
    /// Format a neighbour reference for logs and diagnostics.
    /// </summary>
    public override string ToString()
        => InterfaceName is null
            ? $"{AsNumber}:{RouterName}"
            : $"{AsNumber}:{RouterName}:{InterfaceName}";
}