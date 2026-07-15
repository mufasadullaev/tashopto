using System;
using System.Collections.Generic;
using System.Linq;

namespace Opto.Models;

public enum BaxtaFuelRowKind
{
    LoadBase,
    Load,
    Pug,
    Wak,
    Dop,
    Top,
    Tpp,
    Sn,
    Tpw,
    Total,
    Gkwh,
}

public sealed class BaxtaFuelRow
{
    public required BaxtaFuelRowKind Kind { get; init; }
    public required string Label { get; init; }
    public required string Col1 { get; init; }
    public required string Col2 { get; init; }
    public required string Col3 { get; init; }
    public required string Col4 { get; init; }
    public required string Col5 { get; init; }
    public required string Col6 { get; init; }
    public required string Col7 { get; init; }
    public required string Col8 { get; init; }
    public required string Col9 { get; init; }
    public required string Col10 { get; init; }
    public required string Col11 { get; init; }
    public required string Col12 { get; init; }
    public required string TotalText { get; init; }
}

public sealed class BaxtaShiftSection
{
    public required string Title { get; init; }
    public string? Watch { get; init; }
    public required IReadOnlyList<BaxtaFuelRow> Rows { get; init; }
}

public sealed class BaxtaReport
{
    public required DateTime Date { get; init; }
    public required WyrabotkaMode Mode { get; init; }
    public required IReadOnlyList<BaxtaShiftSection> Sections { get; init; }
    public required decimal Urp { get; init; }
    public required decimal Urm { get; init; }
}

public static class BaxtaCalculator
{
    private const decimal GenerationOverflow = 100_000m;
    private const decimal OwnNeedsOverflow = 1_000_000m;
    private static readonly string[] WatchLetters = ["А", "Б", "В", "Г"];
    private static readonly string[] RowLabels =
    [
        "ЛБ", "НАГРУЗКА", "ПУГ", "ВАК", "ДОП", "ТОП", "ТПП", "СН", "ТПВ", "ВСЕГО", "г/кВт·ч",
    ];

    public static BaxtaReport Calculate(
        DateTime date,
        WyrabotkaMode mode,
        IEnumerable<BaxtaMeterBlockRow> meters,
        IEnumerable<BaxtaThermoRow> thermo,
        BaxtaPlantParamsRow plant,
        IEnumerable<BaxtaBlockCoeffsRow> coeffs)
    {
        var meterList = meters.OrderBy(m => m.Number).ToArray();
        var thermoList = thermo.OrderBy(t => t.BlockNumber).ThenBy(t => t.ShiftIndex).ToArray();
        var coeffList = coeffs.OrderBy(c => c.Number).ToArray();

        var wyr = new decimal[12, 3];
        var esn = new decimal[12, 3];
        ComputeDeltas(meterList, wyr, esn);
        ApplyNetworkCorrections(plant, esn);

        var cif = new decimal[12, 11, 4];
        var mtcb = new[] { plant.Tcb1, plant.Tcb2, plant.Tcb3 };

        for (var shift = 1; shift <= 3; shift++)
        {
            for (var block = 1; block <= 12; block++)
            {
                var thermoRow = thermoList.FirstOrDefault(t =>
                    t.BlockNumber == block && t.ShiftIndex == shift);
                if (thermoRow is null || thermoRow.Hours == 0)
                    continue;

                var wy = wyr[block - 1, shift - 1];
                var sn = esn[block - 1, shift - 1];
                var load = wy / thermoRow.Hours;
                var coeff = coeffList[block - 1];
                var pris = plant.Prises[block - 1];
                var regime = GetRegimeAddend(block, shift, plant);

                ComputeFuel(
                    block,
                    shift,
                    wy,
                    sn,
                    load,
                    thermoRow,
                    coeff,
                    pris,
                    mtcb[shift - 1],
                    regime,
                    out var eksut1,
                    out var eksut2,
                    out var eksut3,
                    out var eksut4,
                    out var eksut5,
                    out var eksut6,
                    out var eksut7);

                cif[block - 1, 0, shift - 1] = thermoRow.Tn;
                cif[block - 1, 1, shift - 1] = Round0(load);
                cif[block - 1, 2, shift - 1] = Round0(eksut1);
                cif[block - 1, 3, shift - 1] = Round0(eksut2);
                cif[block - 1, 4, shift - 1] = Round0(eksut3);
                cif[block - 1, 5, shift - 1] = Round0(eksut4);
                cif[block - 1, 6, shift - 1] = Round0(eksut5);
                cif[block - 1, 7, shift - 1] = Round0(eksut6);
                cif[block - 1, 8, shift - 1] = Round0(eksut7);

                for (var row = 2; row <= 8; row++)
                    cif[block - 1, row, 3] += cif[block - 1, row, shift - 1];
            }
        }

        for (var block = 0; block < 12; block++)
        {
            for (var shift = 0; shift < 4; shift++)
            {
                var sum = 0m;
                for (var row = 2; row <= 8; row++)
                    sum += cif[block, row, shift];
                cif[block, 9, shift] = sum;
            }
        }

        var gkbt = new decimal[13, 4];
        for (var shift = 0; shift < 3; shift++)
        {
            var loadSum = 0m;
            var gkSum = 0m;
            var count = 0;
            for (var block = 0; block < 12; block++)
            {
                var wy = wyr[block, shift];
                if (cif[block, 1, shift] == 0)
                    continue;

                gkbt[block, shift] = wy != 0 ? Round2(cif[block, 9, shift] / wy) : 0;
                loadSum += cif[block, 1, shift];
                gkSum += gkbt[block, shift];
                count++;
            }

            gkbt[12, shift] = count > 0 ? Round2(gkSum / count) : 0;
        }

        for (var block = 0; block < 13; block++)
        {
            var sum = 0m;
            var count = 0;
            for (var shift = 0; shift < 3; shift++)
            {
                if (gkbt[block, shift] == 0)
                    continue;
                sum += gkbt[block, shift];
                count++;
            }

            gkbt[block, 3] = count > 0 ? Round2(sum / count) : 0;
        }

        for (var block = 0; block < 12; block++)
            cif[block, 10, 0] = gkbt[block, 0];
        for (var block = 0; block < 12; block++)
            cif[block, 10, 1] = gkbt[block, 1];
        for (var block = 0; block < 12; block++)
            cif[block, 10, 2] = gkbt[block, 2];
        for (var block = 0; block < 12; block++)
            cif[block, 10, 3] = gkbt[block, 3];
        gkbt[12, 3] = gkbt[12, 3];

        var sections = new List<BaxtaShiftSection>();
        for (var shift = 1; shift <= 4; shift++)
        {
            var shiftIdx = shift - 1;
            sections.Add(new BaxtaShiftSection
            {
                Title = shift == 4 ? "ЗА СУТКИ" : $"{shift}-СМЕНА",
                Watch = shift <= 3 ? WatchLetters[shift - 1] : null,
                Rows = BuildRows(cif, shiftIdx, wyr, gkbt),
            });
        }

        return new BaxtaReport
        {
            Date = date,
            Mode = mode,
            Sections = sections,
            Urp = plant.Urp,
            Urm = plant.Urm,
        };
    }

    private static IReadOnlyList<BaxtaFuelRow> BuildRows(
        decimal[,,] cif,
        int shiftIdx,
        decimal[,] wyr,
        decimal[,] gkbt)
    {
        var rows = new List<BaxtaFuelRow>();
        for (var row = 0; row < 11; row++)
        {
            var values = new decimal[12];
            for (var block = 0; block < 12; block++)
                values[block] = row == 10 ? gkbt[block, shiftIdx] : cif[block, row, shiftIdx];

            var total = row switch
            {
                1 => AverageNonZero(values),
                10 => gkbt[12, shiftIdx],
                _ => values.Sum(),
            };

            rows.Add(new BaxtaFuelRow
            {
                Kind = (BaxtaFuelRowKind)row,
                Label = RowLabels[row],
                Col1 = FormatValue((BaxtaFuelRowKind)row, values[0]),
                Col2 = FormatValue((BaxtaFuelRowKind)row, values[1]),
                Col3 = FormatValue((BaxtaFuelRowKind)row, values[2]),
                Col4 = FormatValue((BaxtaFuelRowKind)row, values[3]),
                Col5 = FormatValue((BaxtaFuelRowKind)row, values[4]),
                Col6 = FormatValue((BaxtaFuelRowKind)row, values[5]),
                Col7 = FormatValue((BaxtaFuelRowKind)row, values[6]),
                Col8 = FormatValue((BaxtaFuelRowKind)row, values[7]),
                Col9 = FormatValue((BaxtaFuelRowKind)row, values[8]),
                Col10 = FormatValue((BaxtaFuelRowKind)row, values[9]),
                Col11 = FormatValue((BaxtaFuelRowKind)row, values[10]),
                Col12 = FormatValue((BaxtaFuelRowKind)row, values[11]),
                TotalText = FormatValue((BaxtaFuelRowKind)row, total),
            });
        }

        return rows;
    }

    private static void ComputeDeltas(
        BaxtaMeterBlockRow[] meters,
        decimal[,] wyr,
        decimal[,] esn)
    {
        for (var i = 0; i < meters.Length; i++)
        {
            var m = meters[i];
            var gen = new[] { m.GenerationAt0, m.GenerationAt8, m.GenerationAt16, m.GenerationAt24 };
            var sn = new[] { m.OwnNeedsAt0, m.OwnNeedsAt8, m.OwnNeedsAt16, m.OwnNeedsAt24 };

            for (var shift = 0; shift < 3; shift++)
            {
                var gDelta = Delta(gen[shift + 1], gen[shift], GenerationOverflow);
                var sDelta = Delta(sn[shift + 1], sn[shift], OwnNeedsOverflow);
                wyr[i, shift] = m.GenerationCoefficient * 0.001m * gDelta;
                esn[i, shift] = m.OwnNeedsCoefficient * 0.001m * sDelta;
            }
        }
    }

    private static void ApplyNetworkCorrections(BaxtaPlantParamsRow plant, decimal[,] esn)
    {
        var kf = new decimal[5];
        kf[0] = 2880m;
        kf[1] = 2880m;
        kf[2] = 2880m;
        kf[3] = 2880m;
        kf[4] = 2880m;

        if (plant.Setn5K != plant.Setn5N)
            esn[8, 2] -= (plant.Setn5K - plant.Setn5N) * kf[4] * 0.001m / 3m;
        if (plant.Setn1K != plant.Setn1N)
            esn[10, 2] -= (plant.Setn1K - plant.Setn1N) * kf[0] * 0.001m / 3m;

        var set234 =
            (plant.Setn2K - plant.Setn2N) * kf[1] +
            (plant.Setn3K - plant.Setn3N) * kf[2] +
            (plant.Setn4K - plant.Setn4N) * kf[3];
        esn[11, 2] -= set234 * 0.001m / 3m;
    }

    private static decimal GetRegimeAddend(int block, int shift, BaxtaPlantParamsRow plant) =>
        block switch
        {
            8 => 0.5m * shift switch
            {
                1 => plant.Wrmn11,
                2 => plant.Wrmn12,
                _ => plant.Wrmn13,
            },
            9 => 0.5m * shift switch
            {
                1 => plant.Wrmn21,
                2 => plant.Wrmn22,
                _ => plant.Wrmn23,
            },
            10 => 0.5m * shift switch
            {
                1 => plant.Wrmn31,
                2 => plant.Wrmn32,
                _ => plant.Wrmn33,
            },
            11 or 12 => 0.5m * shift switch
            {
                1 => plant.Wrmn14,
                2 => plant.Wrmn24,
                _ => plant.Wrmn34,
            },
            _ => 0m,
        };

    private static void ComputeFuel(
        int block,
        int shift,
        decimal wy,
        decimal sn,
        decimal load,
        BaxtaThermoRow thermo,
        BaxtaBlockCoeffsRow coeff,
        int pris,
        decimal tcb,
        decimal regimeAddend,
        out decimal eksut1,
        out decimal eksut2,
        out decimal eksut3,
        out decimal eksut4,
        out decimal eksut5,
        out decimal eksut6,
        out decimal eksut7)
    {
        var na = load;
        var rrn = 5.15m + 0.0083m * na + 0.022m * (15.0m - thermo.Thw);
        var rrf = 0m;
        if (na != 0)
            rrf = pris * 0.01m * (decimal)Math.Sqrt((double)(160.0m / na));

        rrf += (21.0m - 0.1m * thermo.O2) / (21.0m - thermo.O2);
        rrf = (rrf * 3.5m + 0.6m) * (thermo.Tug - thermo.Thw) * 0.01m;
        eksut1 = 3.5m * (rrf - rrn) * wy;

        var rr = tcb + 0.07m * na + (20m + na) / (tcb + 17m);
        eksut2 = 0.68m * wy * (thermo.Tk - rr);

        eksut3 = 0m;
        if (thermo.Pop < 126m)
        {
            if (na is >= 80m and <= 117m)
                eksut3 = 0.148m * (na - 80m) * wy;
            else if (na is > 117m and <= 160m)
                eksut3 = 0.127m * (160m - na) * wy;
        }

        eksut4 = -0.07m * wy * (thermo.Top - 540m);

        var rrTpp = na switch
        {
            >= 70m and < 115m => 530m + 0.429m * (na - 80m),
            >= 115m => 540m,
            _ => 530m,
        };
        eksut5 = thermo.Tpp < 540m ? -0.056m * wy * (thermo.Tpp - rrTpp) : 0m;

        var rrSn = coeff.Kf1 * thermo.Hours + coeff.Kf2 * wy + regimeAddend;
        eksut6 = (sn - rrSn) * 350m;

        if (thermo.Pwd == 0)
            eksut7 = (2.98m + 0.0276m * (na - 80m)) * wy;
        else
        {
            var rrTpw = na < 150m
                ? 170m + coeff.Kl1 * na
                : coeff.Kl2 + 3m + 0.2m * (na - 150m);
            if (na >= 160m)
                rrTpw = coeff.Kl2 + 3m + 0.2m * (na - 150m);
            eksut7 = -0.066m * (thermo.Tpw - rrTpw + 0.05m * thermo.Dro) * wy;
        }
    }

    private static decimal Delta(decimal end, decimal start, decimal overflow)
    {
        var delta = end - start;
        return delta < 0 ? delta + overflow : delta;
    }

    private static decimal AverageNonZero(decimal[] values)
    {
        var nonZero = values.Where(v => v != 0).ToArray();
        return nonZero.Length == 0 ? 0 : Round0(nonZero.Average());
    }

    private static decimal Round0(decimal value) =>
        Math.Round(value, 0, MidpointRounding.AwayFromZero);

    private static decimal Round2(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string FormatValue(BaxtaFuelRowKind kind, decimal value) =>
        kind == BaxtaFuelRowKind.Gkwh
            ? Round2(value).ToString("0.00", OptoCulture.Current)
            : Round0(value).ToString("0", OptoCulture.Current);
}
