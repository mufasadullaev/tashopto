using System;
using System.Collections.Generic;
using System.Linq;

namespace Opto.Models;

public sealed class WyrabotkaReportRow
{
    public required string Label { get; init; }
    public decimal? CoefficientGeneration { get; init; }
    public decimal? GenerationStart { get; init; }
    public decimal? GenerationEnd { get; init; }
    public decimal? CoefficientOwnNeeds { get; init; }
    public decimal? OwnNeedsStart { get; init; }
    public decimal? OwnNeedsEnd { get; init; }
    public int? Hours { get; init; }
    public decimal GenerationThousandKwh { get; init; }
    public decimal GenerationMw { get; init; }
    public decimal OwnNeedsThousandKwh { get; init; }
    public decimal OwnNeedsMw { get; init; }
    public decimal Percent { get; init; }
    public bool IsSummary { get; init; }
    public bool IsTransformer { get; init; }
}

public sealed class WyrabotkaReport
{
    public required DateTime Date { get; init; }
    public required WyrabotkaMode Mode { get; init; }
    public required IReadOnlyList<WyrabotkaReportRow> BlockRows { get; init; }
    public required IReadOnlyList<WyrabotkaReportRow> TransformerRows { get; init; }
    public required WyrabotkaReportRow DayTotal { get; init; }
    public required decimal DayRelease { get; init; }
    public required WyrabotkaReportRow MonthTotal { get; init; }
    public required decimal MonthRelease { get; init; }
}

public static class WyrabotkaCalculator
{
    private const decimal GenerationOverflow = 1_000_000m;
    private const decimal OwnNeedsOverflow = 100_000m;

    public static WyrabotkaReport Calculate(
        DateTime date,
        WyrabotkaMode mode,
        IEnumerable<BlockMeterRow> blocks,
        IEnumerable<TransformerMeterRow> transformers,
        WyrabotkaReport? previousMonth = null)
    {
        var blockRows = new List<WyrabotkaReportRow>();
        decimal sumHours = 0;
        decimal sumGeneration = 0;
        decimal sumOwnNeeds = 0;

        foreach (var block in blocks.OrderBy(b => b.Number))
        {
            var hours = block.Hours;
            var genDelta = Delta(block.GenerationEnd, block.GenerationStart, GenerationOverflow);
            var genKwh = block.GenerationCoefficient * genDelta * 0.001m;
            var genMw = hours > 0 ? genKwh / hours : genKwh;

            var snDelta = Delta(block.OwnNeedsEnd, block.OwnNeedsStart, OwnNeedsOverflow);
            var snKwh = block.OwnNeedsCoefficient * snDelta * 0.001m;
            var snMw = hours > 0 ? snKwh / hours : 0m;
            var percent = genKwh != 0 ? snKwh / genKwh * 100m : 0m;

            blockRows.Add(new WyrabotkaReportRow
            {
                Label = block.Number.ToString(),
                CoefficientGeneration = block.GenerationCoefficient,
                GenerationStart = block.GenerationStart,
                GenerationEnd = block.GenerationEnd,
                CoefficientOwnNeeds = block.OwnNeedsCoefficient,
                OwnNeedsStart = block.OwnNeedsStart,
                OwnNeedsEnd = block.OwnNeedsEnd,
                Hours = hours,
                GenerationThousandKwh = Round1(genKwh),
                GenerationMw = Round1(genMw),
                OwnNeedsThousandKwh = Round1(snKwh),
                OwnNeedsMw = Round1(snMw),
                Percent = Round2(percent),
            });

            sumHours += hours;
            sumGeneration += genKwh;
            sumOwnNeeds += snKwh;
        }

        var transformerRows = new List<WyrabotkaReportRow>();
        decimal sumTransformers = 0;

        foreach (var tr in transformers)
        {
            var delta = tr.End - tr.Start;
            var kwh = tr.Coefficient * delta * 0.001m;
            sumTransformers += kwh;

            transformerRows.Add(new WyrabotkaReportRow
            {
                Label = tr.Name,
                CoefficientOwnNeeds = tr.Coefficient,
                OwnNeedsStart = tr.Start,
                OwnNeedsEnd = tr.End,
                OwnNeedsThousandKwh = Round1(kwh),
                IsTransformer = true,
            });
        }

        var snPlusTr = sumOwnNeeds + sumTransformers;
        var dayRelease = sumGeneration - snPlusTr;
        var dayGenMw = sumHours > 0 ? sumGeneration / sumHours : 0m;
        var daySnMw = sumHours > 0 ? sumOwnNeeds / sumHours : 0m;
        var dayPercent = sumGeneration != 0 ? snPlusTr / sumGeneration * 100m : 0m;

        var dayTotal = new WyrabotkaReportRow
        {
            Label = "за сутки",
            Hours = (int)sumHours,
            GenerationThousandKwh = Round1(sumGeneration),
            GenerationMw = Round1(dayGenMw),
            OwnNeedsThousandKwh = Round1(snPlusTr),
            OwnNeedsMw = Round1(daySnMw),
            Percent = Round2(dayPercent),
            IsSummary = true,
        };

        // Пока нет архива месяца — нарастающий итог = сутки.
        // После подключения хранения сюда добавятся накопленные значения.
        var monthHours = sumHours + (previousMonth?.MonthTotal.Hours ?? 0);
        var monthGeneration = sumGeneration + (previousMonth?.MonthTotal.GenerationThousandKwh ?? 0);
        // Month totals in karat store raw before rounding; for UI use rounded day + previous displayed.
        // Simpler: accumulate raw day values only for now.
        var monthSnPlusTr = snPlusTr;
        var monthRelease = dayRelease;
        var monthGenMw = monthHours > 0 ? monthGeneration / monthHours : 0m;
        var monthSnMw = monthHours > 0 ? sumOwnNeeds / monthHours : 0m;
        var monthPercent = monthGeneration != 0 ? monthSnPlusTr / monthGeneration * 100m : 0m;

        var monthTotal = new WyrabotkaReportRow
        {
            Label = "с начала месяца",
            Hours = (int)monthHours,
            GenerationThousandKwh = Round1(monthGeneration),
            GenerationMw = Round1(monthGenMw),
            OwnNeedsThousandKwh = Round1(monthSnPlusTr),
            OwnNeedsMw = Round1(monthSnMw),
            Percent = Round2(monthPercent),
            IsSummary = true,
        };

        return new WyrabotkaReport
        {
            Date = date,
            Mode = mode,
            BlockRows = blockRows,
            TransformerRows = transformerRows,
            DayTotal = dayTotal,
            DayRelease = Round2(dayRelease),
            MonthTotal = monthTotal,
            MonthRelease = Round2(monthRelease),
        };
    }

    private static decimal Delta(decimal end, decimal start, decimal overflow)
    {
        var delta = end - start;
        return delta < 0 ? delta + overflow : delta;
    }

    private static decimal Round1(decimal value) =>
        Math.Round(value, 1, MidpointRounding.AwayFromZero);

    private static decimal Round2(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
