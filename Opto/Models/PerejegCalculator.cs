using System;
using System.Collections.Generic;
using System.Linq;

namespace Opto.Models;

public static class PerejegInputFactory
{
    public static List<PerejegInputRow> CreateEmptyRows()
    {
        var rows = new List<PerejegInputRow>();
        foreach (var definition in PerejegDefinitions.InputRows)
        {
            rows.Add(new PerejegInputRow
            {
                Index = definition.Index,
                Label = definition.Label,
                Scope = definition.Scope,
            });
        }

        return rows;
    }
}

public static class PerejegCalculator
{
    public static PerejegReport Calculate(
        DateTime date,
        WyrabotkaMode mode,
        IReadOnlyList<PerejegInputRow> rows)
    {
        var computed = ComputeDeviationData(rows);
        return BuildReport(
            date,
            mode,
            computed,
            isCumulative: false);
    }

    public static PerejegReport BuildCumulativeReport(
        DateTime from,
        DateTime to,
        IReadOnlyList<PerejegResultData> dailyResults)
    {
        var deviationKg = new decimal[7][];
        for (var dev = 0; dev < 7; dev++)
            deviationKg[dev] = new decimal[12];

        var blockTotals = new decimal[12];
        var stationRouKg = 0m;
        var releaseSum = 0m;

        foreach (var day in dailyResults)
        {
            releaseSum += day.ReleaseMlnKwh;
            stationRouKg += day.StationRouKg;

            for (var dev = 0; dev < 7; dev++)
            {
                for (var block = 0; block < 12; block++)
                    deviationKg[dev][block] += day.DeviationKg[dev][block];
            }

            for (var block = 0; block < 12; block++)
                blockTotals[block] += day.BlockTotalsKg[block];
        }

        var totalKg = blockTotals.Sum() + stationRouKg;
        var totalGkwh = releaseSum > 0 ? totalKg / releaseSum / 1000m : 0m;

        var computed = new PerejegComputedData
        {
            DeviationKg = deviationKg,
            BlockTotals = blockTotals,
            StationRouKg = stationRouKg,
            ReleaseMlnKwh = releaseSum,
            TotalGkwh = Round2(totalGkwh),
        };

        return BuildReport(
            to,
            WyrabotkaMode.Calculation,
            computed,
            isCumulative: true,
            periodFrom: from,
            periodTo: to,
            displayInTons: true);
    }

    public static PerejegResultData BuildResultData(PerejegReport report)
    {
        var deviationKg = new decimal[7][];
        var blockTotals = new decimal[12];
        decimal stationRou = 0m;

        for (var i = 0; i < 7; i++)
        {
            deviationKg[i] = new decimal[12];
            var row = report.Rows[i];
            for (var block = 0; block < 12; block++)
                deviationKg[i][block] = ParseCell(row, block + 1);

            if (i == 5)
                stationRou = ParseCell(row, 13);
        }

        var totalRow = report.Rows[7];
        for (var block = 0; block < 12; block++)
            blockTotals[block] = ParseCell(totalRow, block + 1);

        return new PerejegResultData
        {
            ReleaseMlnKwh = report.ReleaseMlnKwh,
            TotalGkwh = report.TotalGkwh,
            StationRouKg = stationRou,
            DeviationKg = deviationKg,
            BlockTotalsKg = blockTotals,
        };
    }

    private static PerejegComputedData ComputeDeviationData(IReadOnlyList<PerejegInputRow> rows)
    {
        var power = BuildMatrix(rows, 1, 7);
        var time = BuildMatrix(rows, 8, 14);
        var deltaT = rows[14].BlockValues;
        var release = rows[15].Station;

        var deviationKg = new decimal[7][];
        for (var dev = 0; dev < 7; dev++)
        {
            deviationKg[dev] = new decimal[12];
            var coeff = PerejegDefinitions.DeviationCoefficients[dev];

            for (var block = 0; block < 12; block++)
            {
                var n = power[dev, block];
                var t = time[dev, block];

                deviationKg[dev][block] = dev switch
                {
                    5 => 0m,
                    6 => coeff * n * t * deltaT[block] / 10_000m,
                    _ => coeff * n * t,
                };
            }
        }

        var stationRouKg = PerejegDefinitions.DeviationCoefficients[5] * power[5, 12] * time[5, 12];

        var blockTotals = new decimal[12];
        for (var block = 0; block < 12; block++)
        {
            for (var dev = 0; dev < 7; dev++)
            {
                if (dev == 5)
                    continue;

                blockTotals[block] += deviationKg[dev][block];
            }
        }

        var totalKg = blockTotals.Sum() + stationRouKg;
        var totalGkwh = release > 0 ? totalKg / release / 1000m : 0m;

        return new PerejegComputedData
        {
            DeviationKg = deviationKg,
            BlockTotals = blockTotals,
            StationRouKg = stationRouKg,
            ReleaseMlnKwh = release,
            TotalGkwh = Round2(totalGkwh),
        };
    }

    private static PerejegReport BuildReport(
        DateTime date,
        WyrabotkaMode mode,
        PerejegComputedData computed,
        bool isCumulative,
        DateTime? periodFrom = null,
        DateTime? periodTo = null,
        bool displayInTons = false)
    {
        var reportRows = new List<PerejegReportRow>();
        for (var dev = 0; dev < 7; dev++)
        {
            var blocks = dev == 5 ? null : computed.DeviationKg[dev];
            var rowSum = blocks?.Sum() ?? 0m;
            reportRows.Add(CreateDeviationRow(
                PerejegDefinitions.DeviationLabels[dev],
                blocks,
                dev == 5 ? computed.StationRouKg : rowSum,
                displayInTons));
        }

        reportRows.Add(CreateDeviationRow(
            displayInTons ? "Итого, т у.т." : PerejegDefinitions.DeviationLabels[7],
            computed.BlockTotals,
            computed.BlockTotals.Sum() + computed.StationRouKg,
            displayInTons,
            isSummary: true));

        reportRows.Add(CreateGkwhRow(PerejegDefinitions.DeviationLabels[8], computed.TotalGkwh));

        return new PerejegReport
        {
            Date = date,
            Mode = mode,
            Rows = reportRows,
            ReleaseMlnKwh = computed.ReleaseMlnKwh,
            TotalGkwh = computed.TotalGkwh,
            IsCumulative = isCumulative,
            PeriodFrom = periodFrom,
            PeriodTo = periodTo,
            DisplayInTons = displayInTons,
        };
    }

    private sealed class PerejegComputedData
    {
        public required decimal[][] DeviationKg { get; init; }
        public required decimal[] BlockTotals { get; init; }
        public required decimal StationRouKg { get; init; }
        public required decimal ReleaseMlnKwh { get; init; }
        public required decimal TotalGkwh { get; init; }
    }

    private static decimal[,] BuildMatrix(IReadOnlyList<PerejegInputRow> rows, int fromIndex, int toIndex)
    {
        var matrix = new decimal[7, 13];

        for (var rowIndex = fromIndex; rowIndex <= toIndex; rowIndex++)
        {
            var row = rows[rowIndex - 1];
            var dev = rowIndex <= 7 ? rowIndex - 1 : rowIndex - 8;

            for (var block = 1; block <= 12; block++)
                matrix[dev, block - 1] = row.GetBlock(block);

            matrix[dev, 12] = row.Station;
        }

        return matrix;
    }

    private static PerejegReportRow CreateDeviationRow(
        string label,
        decimal[]? blockValues,
        decimal stationValue,
        bool displayInTons,
        bool isSummary = false)
    {
        blockValues ??= new decimal[12];

        return new PerejegReportRow
        {
            Label = label,
            Col1 = FormatValue(blockValues[0], displayInTons),
            Col2 = FormatValue(blockValues[1], displayInTons),
            Col3 = FormatValue(blockValues[2], displayInTons),
            Col4 = FormatValue(blockValues[3], displayInTons),
            Col5 = FormatValue(blockValues[4], displayInTons),
            Col6 = FormatValue(blockValues[5], displayInTons),
            Col7 = FormatValue(blockValues[6], displayInTons),
            Col8 = FormatValue(blockValues[7], displayInTons),
            Col9 = FormatValue(blockValues[8], displayInTons),
            Col10 = FormatValue(blockValues[9], displayInTons),
            Col11 = FormatValue(blockValues[10], displayInTons),
            Col12 = FormatValue(blockValues[11], displayInTons),
            StationText = FormatValue(stationValue, displayInTons),
            IsSummary = isSummary,
        };
    }

    private static PerejegReportRow CreateGkwhRow(string label, decimal totalGkwh) =>
        new()
        {
            Label = label,
            Col1 = "",
            Col2 = "",
            Col3 = "",
            Col4 = "",
            Col5 = "",
            Col6 = "",
            Col7 = "",
            Col8 = "",
            Col9 = "",
            Col10 = "",
            Col11 = "",
            Col12 = "",
            StationText = totalGkwh > 0 ? totalGkwh.ToString("0.00", OptoCulture.Current) : "",
            IsSummary = true,
        };

    private static decimal ParseCell(PerejegReportRow row, int column) => column switch
    {
        1 => ParseDecimal(row.Col1),
        2 => ParseDecimal(row.Col2),
        3 => ParseDecimal(row.Col3),
        4 => ParseDecimal(row.Col4),
        5 => ParseDecimal(row.Col5),
        6 => ParseDecimal(row.Col6),
        7 => ParseDecimal(row.Col7),
        8 => ParseDecimal(row.Col8),
        9 => ParseDecimal(row.Col9),
        10 => ParseDecimal(row.Col10),
        11 => ParseDecimal(row.Col11),
        12 => ParseDecimal(row.Col12),
        13 => ParseDecimal(row.StationText),
        _ => 0m,
    };

    private static decimal ParseDecimal(string text) =>
        decimal.TryParse(text, out var value) ? value : 0m;

    private static string FormatValue(decimal kgValue, bool displayInTons)
    {
        if (kgValue == 0m)
            return "";

        if (displayInTons)
        {
            var tons = (int)Math.Floor((kgValue + 500m) / 1000m);
            return tons == 0 ? "" : tons.ToString("0", OptoCulture.Current);
        }

        return Math.Round(kgValue, 0, MidpointRounding.AwayFromZero).ToString("0", OptoCulture.Current);
    }

    private static decimal Round2(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
