using System;
using System.Collections.Generic;
using System.Linq;

namespace TrapHub.Core;

/// <summary>
/// The order Take All walks the traps in: the player's own floor first, then the floor that is the fewest floor changes
/// away from the last one, and so on. On each floor it goes to the nearest trap, then the nearest to that one.
/// </summary>
public static class TakeAllRoute
{
    /// <summary>Whether a trap has a catch or a store to empty, and no walk to it is lined up already.</summary>
    public static bool HasCatch(Trap trap) => (trap.PreyId > 0 || trap.StoreCount > 0) && !trap.IsTakeQueued;

    /// <param name="onlyFloor">Null for every floor.</param>
    public static IReadOnlyList<Trap> Plan(IEnumerable<Trap> traps, int playerFloor, Spot playerPosition, Func<int, int, int> floorCost, int? onlyFloor)
    {
        var byFloor = traps.Where(trap => HasCatch(trap) && (onlyFloor == null || trap.Floor == onlyFloor)).GroupBy(trap => trap.Floor).ToDictionary(group => group.Key, group => group.ToList());
        var route = new List<Trap>();
        var floor = playerFloor;
        var position = playerPosition;

        while (byFloor.Count > 0)
        {
            var from = floor;
            floor = byFloor.Keys.OrderBy(next => next == from ? -1 : floorCost(from, next)).ThenBy(next => Math.Abs(next - from)).ThenBy(next => next).First();

            foreach (var trap in Nearest(byFloor[floor], position)) route.Add(trap);

            position = route[route.Count - 1].Position;
            byFloor.Remove(floor);
        }

        return route;
    }

    private static IEnumerable<Trap> Nearest(List<Trap> left, Spot from)
    {
        var position = from;

        while (left.Count > 0)
        {
            var next = left.OrderBy(trap => Distance(trap.Position, position)).ThenBy(trap => trap.Id).First();

            left.Remove(next);
            position = next.Position;

            yield return next;
        }
    }

    private static float Distance(Spot a, Spot b) => (a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z);
}
