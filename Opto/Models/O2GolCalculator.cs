using System;
using System.Collections.Generic;
using System.Linq;

namespace Opto.Models;

public static class O2GolCalculator
{
    private static readonly string[] WatchHeaders = ["А", "Б", "В", "Г"];
    private static readonly string[] SummaryRowLabels =
    [
        "ВЫРАБОТКА (МВт·ч)",
        "НАГРУЗКА (МВт)",
        "ПУГ",
        "ВАК",
        "ДОП",
        "ТОП",
        "ТПП",
        "СН",
        "ТПВ",
        "ВСЕГО",
        "г/кВт·ч",
    ];

    public static O2GolReport Build(DateTime from, DateTime to, IReadOnlyList<BaxtaShiftDetail> details)
    {
        var byBlocks = BuildBlockSection(details);
        var byWatches = BuildWatchSection(details);
        var operators = BuildOperatorGroups(details);

        return new O2GolReport
        {
            From = from.Date,
            To = to.Date,
            ByBlocks = byBlocks,
            ByWatches = byWatches,
            OperatorGroups = operators,
        };
    }

    private static O2GolSummarySection BuildBlockSection(IReadOnlyList<BaxtaShiftDetail> details)
    {
        var fuel = CreateMatrix(12, 7);
        var loadSum = new double[13];
        var genSum = new double[13];
        var loadCount = new int[13];

        Accumulate(details, fuel, loadSum, genSum, loadCount, byBlock: true);

        var rows = BuildSummaryRows(fuel, loadSum, genSum, loadCount, columnCount: 12, byBlock: true);
        return new O2GolSummarySection
        {
            Title = "АНАЛИЗ РАСХОДА ТОПЛИВА (в т у.т.) ПО БЛОКАМ",
            ColumnHeaders = Enumerable.Range(1, 12).Select(i => i.ToString(OptoCulture.Current)).ToArray(),
            Rows = rows,
        };
    }

    private static O2GolSummarySection BuildWatchSection(IReadOnlyList<BaxtaShiftDetail> details)
    {
        var fuel = CreateMatrix(4, 7);
        var loadSum = new double[5];
        var genSum = new double[5];
        var loadCount = new int[5];

        Accumulate(details, fuel, loadSum, genSum, loadCount, byBlock: false);

        var rows = BuildSummaryRows(fuel, loadSum, genSum, loadCount, columnCount: 4, byBlock: false);
        return new O2GolSummarySection
        {
            Title = "АНАЛИЗ РАСХОДА ТОПЛИВА (в т у.т.) ПО ВАХТАМ",
            ColumnHeaders = WatchHeaders,
            Rows = rows,
        };
    }

    private static void Accumulate(
        IReadOnlyList<BaxtaShiftDetail> details,
        double[,] fuel,
        double[] loadSum,
        double[] genSum,
        int[] loadCount,
        bool byBlock)
    {
        foreach (var detail in details)
        {
            var index = byBlock ? detail.BlockNumber : detail.WatchIndex;
            if (index < 1 || index > (byBlock ? 12 : 4))
                continue;

            fuel[index, 0] += detail.Pug / 1000.0;
            fuel[index, 1] += detail.Wak / 1000.0;
            fuel[index, 2] += detail.Dop / 1000.0;
            fuel[index, 3] += detail.Top / 1000.0;
            fuel[index, 4] += detail.Tpp / 1000.0;
            fuel[index, 5] += detail.Sn / 1000.0;
            fuel[index, 6] += detail.Tpw / 1000.0;

            loadSum[index] += detail.Load;
            genSum[index] += detail.Load * 8.0;
            loadCount[index]++;
        }
    }

    private static IReadOnlyList<O2GolSummaryRow> BuildSummaryRows(
        double[,] fuel,
        double[] loadSum,
        double[] genSum,
        int[] loadCount,
        int columnCount,
        bool byBlock)
    {
        var totalFuel = new double[7];
        var totalGen = genSum.Skip(1).Take(columnCount).Sum();
        var totalLoadValues = new List<double>();

        for (var col = 1; col <= columnCount; col++)
        {
            if (loadCount[col] > 0)
                loadSum[col] /= loadCount[col];

            loadSum[col] = FoxRound0(loadSum[col]);
            genSum[col] = FoxRound0(genSum[col]);

            var total = 0.0;
            for (var row = 0; row < 7; row++)
            {
                total += fuel[col, row];
                totalFuel[row] += fuel[col, row];
            }

            fuel[col, 7] = total;

            if (genSum[col] != 0)
                fuel[col, 8] = total * 1000.0 / genSum[col];

            if (loadSum[col] != 0)
                totalLoadValues.Add(loadSum[col]);
        }

        totalGen = FoxRound0(totalGen);
        var stationLoad = byBlock
            ? totalLoadValues.Count > 0 ? FoxRound0(totalLoadValues.Average()) : 0
            : totalLoadValues.Count > 0 ? FoxRound0(totalLoadValues.Sum() / 4.0) : 0;

        var stationTotalFuel = totalFuel.Sum();
        var stationGkwh = totalGen != 0 ? stationTotalFuel * 1000.0 / totalGen : 0;

        var rows = new List<O2GolSummaryRow>();
        for (var rowIndex = 0; rowIndex < SummaryRowLabels.Length; rowIndex++)
        {
            var values = new string[columnCount];
            for (var col = 1; col <= columnCount; col++)
            {
                values[col - 1] = rowIndex switch
                {
                    0 => FormatInt(genSum[col]),
                    1 => FormatInt(loadSum[col]),
                    10 => FormatGkwh(fuel[col, 8]),
                    9 => FormatFuel99(fuel[col, 7]),
                    _ => FormatFuel99(fuel[col, rowIndex - 2]),
                };
            }

            var totalText = rowIndex switch
            {
                0 => FormatInt(totalGen),
                1 => FormatInt(stationLoad),
                10 => FormatGkwh(stationGkwh),
                9 => FormatFuel99(stationTotalFuel),
                _ => FormatFuel99(totalFuel[rowIndex - 2]),
            };

            rows.Add(new O2GolSummaryRow
            {
                Label = SummaryRowLabels[rowIndex],
                Values = values,
                TotalText = totalText,
            });
        }

        return rows;
    }

    private static IReadOnlyList<O2GolOperatorGroup> BuildOperatorGroups(IReadOnlyList<BaxtaShiftDetail> details)
    {
        var groups = details
            .GroupBy(d => (d.OperatorTn, d.BlockNumber))
            .Select(g =>
            {
                var shiftCount = g.Count();
                var loadSum = g.Sum(x => x.Load);
                var avgLoad = shiftCount > 0 ? loadSum / shiftCount : 0;
                var generation = avgLoad * 8.0 * shiftCount;

                var pug = g.Sum(x => x.Pug) * 0.001;
                var wak = g.Sum(x => x.Wak) * 0.001;
                var dop = g.Sum(x => x.Dop) * 0.001;
                var top = g.Sum(x => x.Top) * 0.001;
                var tpp = g.Sum(x => x.Tpp) * 0.001;
                var sn = g.Sum(x => x.Sn) * 0.001;
                var tpw = g.Sum(x => x.Tpw) * 0.001;
                var total = pug + wak + dop + top + tpp + sn + tpw;
                var gkwh = generation != 0 ? total * 1000.0 / generation : 0;

                return new O2GolOperatorRow
                {
                    OperatorTn = g.Key.OperatorTn,
                    BlockNumber = g.Key.BlockNumber,
                    ShiftCount = shiftCount,
                    GenerationText = FormatGen1(generation),
                    LoadText = FormatLoad99(avgLoad),
                    PugText = FormatFuel99(pug),
                    WakText = FormatFuel99(wak),
                    DopText = FormatFuel99(dop),
                    TopText = FormatFuel99(top),
                    TppText = FormatFuel99(tpp),
                    SnText = FormatFuel99(sn),
                    TpwText = FormatFuel99(tpw),
                    TotalText = FormatFuel99(total),
                    GkwhText = FormatGkwh(gkwh),
                };
            })
            .OrderBy(r => r.OperatorTn)
            .ThenBy(r => r.BlockNumber)
            .ToList();

        return
        [
            new O2GolOperatorGroup
            {
                Title = "КТЦ1",
                Rows = groups.Where(r => r.BlockNumber <= 6).ToArray(),
            },
            new O2GolOperatorGroup
            {
                Title = "КТЦ2",
                Rows = groups.Where(r => r.BlockNumber > 6).ToArray(),
            },
        ];
    }

    private static double[,] CreateMatrix(int rows, int cols) => new double[rows + 1, cols + 2];

    private static double FoxRound0(double value) =>
        Math.Round(value, 0, MidpointRounding.AwayFromZero);

    private static string FormatInt(double value) =>
        FoxRound0(value).ToString("0", OptoCulture.Current);

    private static string FormatFuel99(double value) =>
        Math.Round(value, 2, MidpointRounding.ToEven).ToString("0.00", OptoCulture.Current);

    private static string FormatGkwh(double value) =>
        Math.Round(value, 2, MidpointRounding.ToEven).ToString("0.00", OptoCulture.Current);

    private static string FormatGen1(double value) =>
        Math.Round(value, 1, MidpointRounding.ToEven).ToString("0.0", OptoCulture.Current);

    private static string FormatLoad99(double value) =>
        Math.Round(value, 2, MidpointRounding.ToEven).ToString("0.00", OptoCulture.Current);
}
