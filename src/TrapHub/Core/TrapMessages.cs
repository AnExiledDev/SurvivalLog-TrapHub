using System;
using System.Collections.Generic;

namespace TrapHub.Core;

/// <summary>The status line for a press, in the player's language.</summary>
public static class TrapMessages
{
    public static string Describe(TrapOutcome outcome, bool isChinese, bool isAuto = false) => outcome switch
    {
        TrapOutcome.NoGame => Texts.NoGame.Pick(isChinese),
        TrapOutcome.NotReady notReady => Texts.NotReady.Say(isChinese, notReady.Reason),
        TrapOutcome.Gone => Texts.Gone.Pick(isChinese),
        TrapOutcome.Baited baited => Baited(baited, isChinese, isAuto),
        TrapOutcome.BaitedOne one => Texts.BaitedOne.Say(isChinese, one.Trap, one.Bait),
        TrapOutcome.NothingToBait => Texts.NothingToBait.Pick(isChinese),
        TrapOutcome.AlreadyBaited already => Texts.AlreadyBaited.Say(isChinese, already.Trap),
        TrapOutcome.CantBait cant => Texts.CantBait.Say(isChinese, cant.Trap),
        TrapOutcome.NoPreset none => Texts.NoPreset.Say(isChinese, none.Trap),
        TrapOutcome.NoBaitLeft left => Texts.NoBaitLeft.Say(isChinese, left.Trap, left.Preset),
        TrapOutcome.Taking taking => Taking(taking, isChinese),
        TrapOutcome.NothingToTake nothing => nothing.AlreadyQueued > 0 ? Texts.AlreadyTaking.Say(isChinese, nothing.AlreadyQueued) : Texts.NothingToTake.Pick(isChinese),
        TrapOutcome.Dismantling dismantling => Texts.Dismantling.Say(isChinese, dismantling.Trap),
        TrapOutcome.Refused refused => Texts.Refused.Say(isChinese, refused.Trap),
        TrapOutcome.Checked check => Checked(check, isChinese),
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "unknown trap outcome"),
    };

    /// <summary>How many traps a check walks to or baits, for the game's popup ("Check x5"); 0 when it did nothing.</summary>
    public static int CheckedCount(TrapOutcome outcome) => outcome is TrapOutcome.Checked check
        ? (check.Take is TrapOutcome.Taking taking ? taking.Count : 0) + (check.Bait is TrapOutcome.Baited baited ? baited.Count : 0)
        : 0;

    /// <summary>Whether the auto pass has anything worth telling the player: only traps it actually baited.</summary>
    public static bool IsWorthShowing(TrapOutcome outcome) => outcome is TrapOutcome.Baited { Count: > 0 };

    private static string Baited(TrapOutcome.Baited baited, bool isChinese, bool isAuto)
    {
        var parts = new List<string> { (isAuto ? Texts.AutoBaited : Texts.BaitedCount).Say(isChinese, baited.Count) };

        if (baited.NoBait > 0) parts.Add(Texts.HadNoBait.Say(isChinese, baited.NoBait));
        if (baited.RefusedCount > 0) parts.Add(Texts.GameRefused.Say(isChinese, baited.RefusedCount));

        return string.Join(" ", parts);
    }

    /// <summary>Only the halves that did something are told, so a check with nothing to collect talks about bait alone.</summary>
    private static string Checked(TrapOutcome.Checked check, bool isChinese)
    {
        var isTakeIdle = check.Take is TrapOutcome.NothingToTake { AlreadyQueued: 0 };
        var isBaitIdle = check.Bait is TrapOutcome.NothingToBait;

        if (isTakeIdle && isBaitIdle) return Texts.NothingToCheck.Pick(isChinese);
        if (isTakeIdle) return Describe(check.Bait, isChinese);
        if (isBaitIdle) return Describe(check.Take, isChinese);

        return Describe(check.Take, isChinese) + " " + Describe(check.Bait, isChinese);
    }

    private static string Taking(TrapOutcome.Taking taking, bool isChinese)
    {
        var text = taking.Count == 0 ? Texts.NothingToTake.Pick(isChinese)
            : taking.Count > 1 && taking.Floors == 1 ? Texts.TakingOneFloor.Say(isChinese, taking.Count)
            : Texts.Taking.Say(isChinese, taking.Count, taking.Floors);

        return taking.RefusedCount > 0 ? text + " " + Texts.GameRefused.Say(isChinese, taking.RefusedCount) : text;
    }
}
