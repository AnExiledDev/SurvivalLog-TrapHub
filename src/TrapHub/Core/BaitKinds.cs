namespace TrapHub.Core;

/// <summary>
/// A bait's kind, read off the start of its effect line (Config_TrapBait.Effect_Des_Local), which the game writes in
/// Chinese whatever the player's language: "广谱诱饵 - ..." for universal bait and so on.
/// </summary>
public static class BaitKinds
{
    public static BaitKind FromEffect(string? effect)
    {
        var text = effect?.TrimStart() ?? "";

        if (text.StartsWith("广谱诱饵", System.StringComparison.Ordinal)) return BaitKind.Universal;
        if (text.StartsWith("偏好诱饵", System.StringComparison.Ordinal)) return BaitKind.Preference;
        if (text.StartsWith("专精诱饵", System.StringComparison.Ordinal)) return BaitKind.Specialized;
        if (text.StartsWith("均衡诱饵", System.StringComparison.Ordinal)) return BaitKind.Balanced;

        return BaitKind.Other;
    }
}
