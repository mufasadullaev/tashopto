using System;
using System.Collections.Generic;
using Opto.Models;

namespace Opto.Services;

public static class O2GolStore
{
    public static O2GolReport? TryBuildReport(DateTime from, DateTime to)
    {
        var details = new List<BaxtaShiftDetail>();

        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            var persisted = BaxtaStore.TryLoadShiftDetails(date);
            if (persisted.Count > 0)
            {
                details.AddRange(persisted);
                continue;
            }

            if (!BaxtaStore.HasResult(date))
                continue;

            var snapshot = BaxtaStore.TryLoad(date);
            if (snapshot is null)
                continue;

            var mode = BaxtaStore.TryLoadMode(date) ?? WyrabotkaMode.Calculation;
            var watches = BaxtaWatchStore.ResolveWatches(date, mode);
            details.AddRange(BaxtaCalculator.CalculateShiftDetails(date, snapshot, watches));
        }

        if (details.Count == 0)
            return null;

        return O2GolCalculator.Build(from, to, details);
    }
}
