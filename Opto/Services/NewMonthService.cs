using System;
using Opto.Models;
using Opto.Services;

namespace Opto.Services;

public static class NewMonthService
{
    public static string? TryPrepareForNewMonth(bool resetWatchSchedule)
    {
        if (resetWatchSchedule)
            BaxtaWatchStore.ResetForNewMonth();

        var latest = WyrabotkaStore.GetLatestDay();
        if (latest is null)
            return null;

        var snapshot = WyrabotkaStore.TryLoad(latest.Value);
        if (snapshot is null)
            return null;

        foreach (var block in snapshot.Blocks)
            block.Hours = 0;

        foreach (var transformer in snapshot.Transformers)
            transformer.Start = transformer.End;

        var mode = WyrabotkaStore.TryLoadMode(latest.Value) ?? WyrabotkaMode.Calculation;
        WyrabotkaStore.Save(latest.Value, mode, snapshot);
        return null;
    }
}
