using UnityEngine;

namespace Chestworks;

/// <summary>Which containers a purpose reaches for the local player; the reach is everything the parts cover.</summary>
public sealed class ReachRules
{
    /// <summary>Containers within this distance of the character's eye. 0 for none.</summary>
    public float Radius;

    /// <summary>Containers within the build range of a station covering the character or <see cref="Point" />.</summary>
    public bool Stations;

    /// <summary>With <see cref="Stations" />, also every station chained to those by overlapping build ranges.</summary>
    public bool Linked;

    /// <summary>Where the work happens, or null for the character; containers nearest it are used first.</summary>
    public Vector3? Point;
}
