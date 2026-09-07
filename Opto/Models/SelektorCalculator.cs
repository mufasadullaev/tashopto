using System;
using System.Collections.Generic;
using System.Linq;

namespace Opto.Models;

public static class SelektorCalculator
{
    public static SelektorReport Calculate(
        DateTime date,
        WyrabotkaReport? dayWyrabotka)
    {
        var blocks = new List<SelektorBlockStatus>();
        decimal totalGen = 0;
        decimal totalSn = 0;

        if (dayWyrabotka is not null)
        {
            foreach (var b in dayWyrabotka.BlockRows)
            {
                if (int.TryParse(b.Label, out var num))
                {
                    var genMw = b.GenerationMw;
                    var snMw = b.OwnNeedsMw;
                    var hours = b.Hours ?? 0;

                    blocks.Add(new SelektorBlockStatus
                    {
                        BlockNumber = num,
                        GenerationMw = genMw,
                        OwnNeedsMw = snMw,
                        OperatingHours = hours,
                        Comment = hours > 0 ? $"Работает ({hours}ч)" : "Остановлен",
                    });

                    totalGen += genMw;
                    totalSn += snMw;
                }
            }
        }
        else
        {
            for (var i = 1; i <= 12; i++)
            {
                blocks.Add(new SelektorBlockStatus
                {
                    BlockNumber = i,
                    GenerationMw = 0,
                    OwnNeedsMw = 0,
                    OperatingHours = 0,
                    Comment = "Нет данных за указанную дату",
                });
            }
        }

        var release = totalGen - totalSn;

        return new SelektorReport
        {
            Date = date,
            Blocks = blocks,
            TotalGenerationMw = totalGen,
            TotalOwnNeedsMw = totalSn,
            StationReleaseMw = release,
        };
    }
}
