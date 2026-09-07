using System;
using System.Collections.Generic;
using System.Linq;

namespace Opto.Models;

public static class AktCalculator
{
    public static AktReport Calculate(
        DateTime date,
        WyrabotkaReport? monthWyrabotka,
        AktInputSnapshot input)
    {
        var blockRows = new List<AktBlockRow>();
        decimal blockOwnNeedsSum = 0;
        decimal trOwnNeedsSum = 0;
        decimal grossGenSum = 0;

        if (monthWyrabotka is not null)
        {
            foreach (var b in monthWyrabotka.BlockRows)
            {
                if (int.TryParse(b.Label, out var num))
                {
                    var gen = b.GenerationThousandKwh * 1000m;
                    var sn = b.OwnNeedsThousandKwh * 1000m;

                    blockRows.Add(new AktBlockRow
                    {
                        BlockNumber = num,
                        GenerationKwh = gen,
                        OwnNeedsKwh = sn,
                        CompensatorKwh = 0,
                    });

                    grossGenSum += gen;
                    blockOwnNeedsSum += sn;
                }
            }

            foreach (var tr in monthWyrabotka.TransformerRows)
            {
                trOwnNeedsSum += tr.OwnNeedsThousandKwh * 1000m;
            }
        }
        else
        {
            for (var i = 1; i <= 12; i++)
            {
                blockRows.Add(new AktBlockRow
                {
                    BlockNumber = i,
                    GenerationKwh = 0,
                    OwnNeedsKwh = 0,
                    CompensatorKwh = 0,
                });
            }
        }

        var extraOwnNeeds = input.ReserveExciterKwh + input.LossesKwh + input.PlantFacilitiesKwh + input.PreventoriumKwh;
        var totalOwnNeeds = blockOwnNeedsSum + trOwnNeedsSum + extraOwnNeeds;
        var totalGrossGen = grossGenSum + input.ReserveExciterKwh;
        var netRelease = totalGrossGen - totalOwnNeeds;

        var intakeSum = input.IntakeFlows.Sum(f => f.ValueKwh);
        var deliverySum = input.DeliveryFlows.Sum(f => f.ValueKwh);

        return new AktReport
        {
            Date = date,
            Blocks = blockRows,
            ReserveExciterKwh = input.ReserveExciterKwh,
            LossesKwh = input.LossesKwh,
            PlantFacilitiesKwh = input.PlantFacilitiesKwh,
            PreventoriumKwh = input.PreventoriumKwh,
            BlockOwnNeedsTotalKwh = blockOwnNeedsSum,
            TransformerOwnNeedsTotalKwh = trOwnNeedsSum,
            TotalOwnNeedsKwh = totalOwnNeeds,
            GrossGenerationKwh = totalGrossGen,
            NetReleaseKwh = netRelease,
            IntakeFlows = input.IntakeFlows,
            DeliveryFlows = input.DeliveryFlows,
            TotalIntakeKwh = intakeSum,
            TotalDeliveryKwh = deliverySum,
        };
    }
}
