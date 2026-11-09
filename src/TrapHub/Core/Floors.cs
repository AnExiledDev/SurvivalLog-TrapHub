namespace TrapHub.Core;

/// <summary>
/// The home's floors. The game names each by a map id (FloorRouteHelper: 1F is 1, B1 is 101, 2F is 102); Trap Hub
/// sorts and shows them by level instead, B1 as -1, so the basement comes first and the floor list reads bottom up.
/// </summary>
public static class Floors
{
    public const int GroundMap = 1;
    public const int BasementMap = 101;
    public const int UpstairsMap = 102;

    /// <summary>A map id the game may add later keeps its id as its level, so it still sorts and routes somewhere.</summary>
    public static int Level(int mapId) => mapId switch
    {
        BasementMap => -1,
        GroundMap => 1,
        UpstairsMap => 2,
        _ => mapId,
    };

    public static int MapId(int level) => level switch
    {
        -1 => BasementMap,
        1 => GroundMap,
        2 => UpstairsMap,
        _ => level,
    };

    /// <summary>How many floor changes apart two levels are, when the game's route cannot be read. There is no level 0.</summary>
    public static int Steps(int fromLevel, int toLevel)
    {
        var steps = System.Math.Abs(toLevel - fromLevel);

        return (fromLevel < 0) != (toLevel < 0) ? steps - 1 : steps;
    }
}
