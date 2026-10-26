using System;
using System.Collections.Generic;

namespace TrapHub.Core;

/// <summary>
/// What a view the page already has was drawn from: the read and the preset book. A fresh read that matches it item for
/// item would draw the same view, so the view is not built again. Forget it whenever the page may have lost the view or
/// a setting the view shows changed.
/// </summary>
public sealed class DrawnFrom
{
    private readonly bool _isPositionDrawn;
    private TrapWorld? _world;
    private PresetBook? _book;

    /// <param name="isPositionDrawn">Whether the view depends on where the player stands (the Check traps route does).</param>
    public DrawnFrom(bool isPositionDrawn)
    {
        _isPositionDrawn = isPositionDrawn;
    }

    public bool IsSame(TrapWorld world, PresetBook book) =>
        _world != null && ReferenceEquals(book, _book) && IsSameWorld(world, _world, _isPositionDrawn);

    public void Remember(TrapWorld world, PresetBook book)
    {
        _world = world;
        _book = book;
    }

    public void Forget()
    {
        _world = null;
        _book = null;
    }

    /// <summary>Item for item, without allocating: the catalog by reference (it is rebuilt only for a new game or language).</summary>
    public static bool IsSameWorld(TrapWorld a, TrapWorld b, bool isPositionDrawn) =>
        ReferenceEquals(a.Catalog, b.Catalog)
        && a.PlayerFloor == b.PlayerFloor
        && a.SaveKey == b.SaveKey
        && a.Bonus == b.Bonus
        && (!isPositionDrawn || a.PlayerPosition == b.PlayerPosition)
        && IsSameList(a.Traps, b.Traps)
        && IsSameList(a.Stock, b.Stock);

    private static bool IsSameList<T>(IReadOnlyList<T> a, IReadOnlyList<T> b) where T : IEquatable<T>
    {
        if (a.Count != b.Count) return false;

        for (var i = 0; i < a.Count; i++)
        {
            if (!a[i].Equals(b[i])) return false;
        }

        return true;
    }
}
