namespace RouterQuack.Core.Processors.Models;

/// <summary>
/// Parsed reference to a neighbour router, with an optional neighbour interface name.
/// </summary>
/// <param name="AsNumber">AS number that owns the neighbour router.</param>
/// <param name="RouterName">Name of the neighbour router.</param>
/// <param name="InterfaceName">Optional explicit name of the neighbour interface.</param>
public readonly record struct NeighbourReference(int AsNumber, string RouterName, string? InterfaceName);
