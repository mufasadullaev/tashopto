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
    private const double GenerationOverflow = 100_000;
    private const double OwnNeedsOverflow = 1_000_000;
    private static readonly string[] RowLabels =
    [
        "МБ", "НАГРУЗКА", "ПУГ", "ВАК", "ДОП", "ТОП", "ТПП", "СН", "ТПВ", "ВСЕГО", "г/кВт·ч",
    ];

    public static BaxtaReport Calculate(
        DateTime date,
        WyrabotkaMode mode,
        IEnumerable<BaxtaMeterBlockRow> meters,
        IEnumerable<BaxtaThermoRow> thermo,
        BaxtaPlantParamsRow plant,
        IEnumerable<BaxtaBlockCoeffsRow> coeffs,
        int[] watchByShift) =>
        CalculateCore(date, mode, meters, thermo, plant, coeffs, watchByShift).Report;

    public static (BaxtaReport Report, IReadOnlyList<BaxtaShiftDetail> Details) CalculateWithDetails(
        DateTime date,
        WyrabotkaMode mode,
        IEnumerable<BaxtaMeterBlockRow> meters,
        IEnumerable<BaxtaThermoRow> thermo,
        BaxtaPlantParamsRow plant,
        IEnumerable<BaxtaBlockCoeffsRow> coeffs,
        int[] watchByShift) =>
        CalculateCore(date, mode, meters, thermo, plant, coeffs, watchByShift);

    public static IReadOnlyList<BaxtaShiftDetail> CalculateShiftDetails(
        DateTime date,
        BaxtaEditSnapshot snapshot,
        int[] watchByShift) =>
        CalculateShiftDetails(
            snapshot.ToMeterRows(),
            snapshot.ToThermoRows(),
            snapshot.ToPlantRow(),
            snapshot.ToCoeffRows(),
            date,
            watchByShift);

    public static BaxtaReport Calculate(
        DateTime date,
        WyrabotkaMode mode,
        IEnumerable<BaxtaMeterBlockRow> meters,
        IEnumerable<BaxtaThermoRow> thermo,
        BaxtaPlantParamsRow plant,
        IEnumerable<BaxtaBlockCoeffsRow> coeffs) =>
        Calculate(date, mode, meters, thermo, plant, coeffs, DefaultWatchByShift);

    private static int[] DefaultWatchByShift => [1, 2, 3];

    private static (BaxtaReport Report, IReadOnlyList<BaxtaShiftDetail> Details) CalculateCore(
        DateTime date,
        WyrabotkaMode mode,
        IEnumerable<BaxtaMeterBlockRow> meters,
        IEnumerable<BaxtaThermoRow> thermo,
        BaxtaPlantParamsRow plant,
        IEnumerable<BaxtaBlockCoeffsRow> coeffs,
        int[] watchByShift)
    {
        var meterList = meters.OrderBy(m => m.Number).ToArray();
        var thermoList = thermo.OrderBy(t => t.BlockNumber).ThenBy(t => t.ShiftIndex).ToArray();
        var coeffList = coeffs.OrderBy(c => c.Number).ToArray();

        var wyr = new double[12, 3];
        var esn = new double[12, 3];
        ComputeDeltas(meterList, wyr, esn);
        ApplyNetworkPumpCorrections(plant, esn);

        // cif[block, row, shift]: shift 0..2 = смены, 3 = сутки (как karat cif(*,*,4))
        var cif = new double[12, 11, 4];
        var mtcb = new[] { (double)plant.Tcb1, (double)plant.Tcb2, (double)plant.Tcb3 };

        // karat: суточные суммы копят точные eksut*, потом один round
        var dayRaw = new double[12, 9]; // rows 0..8: tn unused, load raw, eksut1..7
        var dayLoadProm = new double[12];
        var dayLoadKol = new int[12];

        var shiftFuels = new Dictionary<(int Block, int Shift), (double Load, double Pug, double Wak, double Dop, double Top, double Tpp, double Sn, double Tpw)>();

        for (var shift = 1; shift <= 3; shift++)
        {
            for (var block = 1; block <= 12; block++)
            {
                var thermoRow = thermoList.FirstOrDefault(t =>
                    t.BlockNumber == block && t.ShiftIndex == shift);
                if (thermoRow is null || thermoRow.Hours == 0)
                    continue;

                var b = block - 1;
                var s = shift - 1;
                var wy = wyr[b, s];
                var sn = esn[b, s];
                var load = wy / thermoRow.Hours;
                var coeff = coeffList[b];
                var pris = plant.Prises[b];
                var regime = GetRegimeAddend(block, shift, plant);

                ComputeFuel(
                    wy,
                    sn,
                    load,
                    thermoRow,
                    coeff,
                    pris,
                    mtcb[s],
                    regime,
                    out var eksut1,
                    out var eksut2,
                    out var eksut3,
                    out var eksut4,
                    out var eksut5,
                    out var eksut6,
                    out var eksut7);

                shiftFuels[(block, shift)] = (load, eksut1, eksut2, eksut3, eksut4, eksut5, eksut6, eksut7);

                cif[b, 0, s] = thermoRow.Tn;
                cif[b, 1, s] = FoxRound0(load);
                cif[b, 2, s] = FoxRound0(eksut1);
                cif[b, 3, s] = FoxRound0(eksut2);
                cif[b, 4, s] = FoxRound0(eksut3);
                cif[b, 5, s] = FoxRound0(eksut4);
                cif[b, 6, s] = FoxRound0(eksut5);
                cif[b, 7, s] = FoxRound0(eksut6);
                cif[b, 8, s] = FoxRound0(eksut7);

                dayLoadProm[b] += load;
                dayLoadKol[b]++;
                dayRaw[b, 2] += eksut1;
                dayRaw[b, 3] += eksut2;
                dayRaw[b, 4] += eksut3;
                dayRaw[b, 5] += eksut4;
                dayRaw[b, 6] += eksut5;
                dayRaw[b, 7] += eksut6;
                dayRaw[b, 8] += eksut7;
            }
        }

        for (var b = 0; b < 12; b++)
        {
            cif[b, 1, 3] = dayLoadKol[b] > 0
                ? FoxRound0(dayLoadProm[b] / dayLoadKol[b])
                : 0;
            for (var row = 2; row <= 8; row++)
                cif[b, row, 3] = FoxRound0(dayRaw[b, row]);
        }

        for (var block = 0; block < 12; block++)
        {
            for (var shift = 0; shift < 4; shift++)
            {
                var sum = 0.0;
                for (var row = 2; row <= 8; row++)
                    sum += cif[block, row, shift];
                cif[block, 9, shift] = sum;
            }
        }

        var gkbt = new double[13, 4];
        for (var shift = 0; shift < 3; shift++)
        {
            var gkSum = 0.0;
            var count = 0;
            for (var block = 0; block < 12; block++)
            {
                var wy = wyr[block, shift];
                if (cif[block, 1, shift] == 0)
                    continue;

                // karat: gkbt = cif(10)/wyr; pict только при выводе
                gkbt[block, shift] = wy != 0 ? cif[block, 9, shift] / wy : 0;
                gkSum += gkbt[block, shift];
                count++;
            }

            gkbt[12, shift] = count > 0 ? gkSum / count : 0;
        }

        for (var block = 0; block < 13; block++)
        {
            var sum = 0.0;
            var count = 0;
            for (var shift = 0; shift < 3; shift++)
            {
                if (gkbt[block, shift] == 0)
                    continue;
                sum += gkbt[block, shift];
                count++;
            }

            gkbt[block, 3] = count > 0 ? sum / count : 0;
        }

        for (var block = 0; block < 12; block++)
        {
            cif[block, 10, 0] = gkbt[block, 0];
            cif[block, 10, 1] = gkbt[block, 1];
            cif[block, 10, 2] = gkbt[block, 2];
            cif[block, 10, 3] = gkbt[block, 3];
        }

        var sections = new List<BaxtaShiftSection>();
        for (var shift = 1; shift <= 4; shift++)
        {
            var shiftIdx = shift - 1;
            sections.Add(new BaxtaShiftSection
            {
                Title = shift == 4 ? "ЗА СУТКИ" : $"{shift}-СМЕНА",
                Watch = shift <= 3 ? BaxtaWatchLetters.FromNumber(watchByShift[shiftIdx]) : null,
                Rows = BuildRows(cif, shiftIdx, gkbt),
            });
        }

        var details = new List<BaxtaShiftDetail>();
        foreach (var (key, fuel) in shiftFuels)
        {
            var thermoRow = thermoList.First(t =>
                t.BlockNumber == key.Block && t.ShiftIndex == key.Shift);
            details.Add(new BaxtaShiftDetail
            {
                Date = date.Date,
                BlockNumber = key.Block,
                ShiftIndex = key.Shift,
                WatchIndex = watchByShift[key.Shift - 1],
                OperatorTn = thermoRow.Tn,
                Load = fuel.Load,
                Pug = fuel.Pug,
                Wak = fuel.Wak,
                Dop = fuel.Dop,
                Top = fuel.Top,
                Tpp = fuel.Tpp,
                Sn = fuel.Sn,
                Tpw = fuel.Tpw,
            });
        }

        var report = new BaxtaReport
        {
            Date = date,
            Mode = mode,
            Sections = sections,
            Urp = plant.Urp,
            Urm = plant.Urm,
        };

        return (report, details);
    }

    public static IReadOnlyList<BaxtaShiftDetail> CalculateShiftDetails(
        IEnumerable<BaxtaMeterBlockRow> meters,
        IEnumerable<BaxtaThermoRow> thermo,
        BaxtaPlantParamsRow plant,
        IEnumerable<BaxtaBlockCoeffsRow> coeffs,
        DateTime date,
        int[] watchByShift) =>
        CalculateCore(date, WyrabotkaMode.Calculation, meters, thermo, plant, coeffs, watchByShift).Details;

    public static IReadOnlyList<BaxtaShiftDetail> CalculateShiftDetails(
        DateTime date,
        BaxtaEditSnapshot snapshot) =>
        CalculateShiftDetails(date, snapshot, DefaultWatchByShift);

    private static IReadOnlyList<BaxtaFuelRow> BuildRows(
        double[,,] cif,
        int shiftIdx,
        double[,] gkbt)
    {
        var rows = new List<BaxtaFuelRow>();
        for (var row = 0; row < 11; row++)
        {
            var values = new double[12];
            for (var block = 0; block < 12; block++)
                values[block] = row == 10 ? gkbt[block, shiftIdx] : cif[block, row, shiftIdx];

            var kind = (BaxtaFuelRowKind)row;
            var totalText = row switch
            {
                0 => string.Empty, // karat procedure MB: no «ПО СТАНЦИИ» column
                1 => FormatValue(kind, AverageNonZero(values)),
                10 => FormatValue(kind, gkbt[12, shiftIdx]),
                _ => FormatValue(kind, values.Sum()),
            };

            rows.Add(new BaxtaFuelRow
            {
                Kind = kind,
                Label = RowLabels[row],
                Col1 = FormatValue(kind, values[0]),
                Col2 = FormatValue(kind, values[1]),
                Col3 = FormatValue(kind, values[2]),
                Col4 = FormatValue(kind, values[3]),
                Col5 = FormatValue(kind, values[4]),
                Col6 = FormatValue(kind, values[5]),
                Col7 = FormatValue(kind, values[6]),
                Col8 = FormatValue(kind, values[7]),
                Col9 = FormatValue(kind, values[8]),
                Col10 = FormatValue(kind, values[9]),
                Col11 = FormatValue(kind, values[10]),
                Col12 = FormatValue(kind, values[11]),
                TotalText = totalText,
            });
        }

        return rows;
    }

    /// <summary>
    /// karat: SET*SM = (ПК − ПН) × KF × 0.001 / 3; коррекция СН блоков 9, 11, 12 на все 3 смены.
    /// KF из wyrab2 (записи 185, 186, 187, 193, 194) — <see cref="NetworkPumpCoefficients.KaratWyrab2Defaults"/>.
    /// </summary>
    private static void ApplyNetworkPumpCorrections(BaxtaPlantParamsRow plant, double[,] esn)
    {
        var kf = (double[])NetworkPumpCoefficients.KaratWyrab2Defaults.Clone();

        var set1 = NetworkPumpShiftShare(plant.Setn1K, plant.Setn1N, kf[0]);
        var set2 = NetworkPumpShiftShare(plant.Setn2K, plant.Setn2N, kf[1]);
        var set3 = NetworkPumpShiftShare(plant.Setn3K, plant.Setn3N, kf[2]);
        var set4 = NetworkPumpShiftShare(plant.Setn4K, plant.Setn4N, kf[3]);
        var set5 = NetworkPumpShiftShare(plant.Setn5K, plant.Setn5N, kf[4]);

        for (var shift = 0; shift < 3; shift++)
        {
            esn[8, shift] -= set5; // блок 9
            esn[10, shift] -= set1; // блок 11
            esn[11, shift] -= set2 + set3 + set4; // блок 12
        }
    }

    private static double NetworkPumpShiftShare(int end, int start, double coefficient) =>
        (end - start) * coefficient * 0.001 / 3.0;

    private static void ComputeDeltas(
        BaxtaMeterBlockRow[] meters,
        double[,] wyr,
        double[,] esn)
    {
        for (var i = 0; i < meters.Length; i++)
        {
            var m = meters[i];
            var gen = new[]
            {
                (double)m.GenerationAt0,
                (double)m.GenerationAt8,
                (double)m.GenerationAt16,
                (double)m.GenerationAt24,
            };
            var sn = new[]
            {
                (double)m.OwnNeedsAt0,
                (double)m.OwnNeedsAt8,
                (double)m.OwnNeedsAt16,
                (double)m.OwnNeedsAt24,
            };

            for (var shift = 0; shift < 3; shift++)
            {
                var gDelta = Delta(gen[shift + 1], gen[shift], GenerationOverflow);
                var sDelta = Delta(sn[shift + 1], sn[shift], OwnNeedsOverflow);
                wyr[i, shift] = (double)m.GenerationCoefficient * 0.001 * gDelta;
                esn[i, shift] = (double)m.OwnNeedsCoefficient * 0.001 * sDelta;
            }
        }
    }

    private static double GetRegimeAddend(int block, int shift, BaxtaPlantParamsRow plant) =>
        block switch
        {
            8 => 0.5 * shift switch
            {
                1 => plant.Wrmn11,
                2 => plant.Wrmn12,
                _ => plant.Wrmn13,
            },
            9 => 0.5 * shift switch
            {
                1 => plant.Wrmn21,
                2 => plant.Wrmn22,
                _ => plant.Wrmn23,
            },
            10 => 0.5 * shift switch
            {
                1 => plant.Wrmn31,
                2 => plant.Wrmn32,
                _ => plant.Wrmn33,
            },
            11 or 12 => 0.5 * shift switch
            {
                1 => plant.Wrmn14,
                2 => plant.Wrmn24,
                _ => plant.Wrmn34,
            },
            _ => 0,
        };

    private static void ComputeFuel(
        double wy,
        double sn,
        double load,
        BaxtaThermoRow thermo,
        BaxtaBlockCoeffsRow coeff,
        int pris,
        double tcb,
        double regimeAddend,
        out double eksut1,
        out double eksut2,
        out double eksut3,
        out double eksut4,
        out double eksut5,
        out double eksut6,
        out double eksut7)
    {
        var na = load;
        var rrn = 5.15 + 0.0083 * na + 0.022 * (15.0 - (double)thermo.Thw);
        var rrf = 0.0;
        if (na != 0)
            rrf = pris * 0.01 * Math.Sqrt(160.0 / na);

        var denomO2 = 21.0 - (double)thermo.O2;
        if (Math.Abs(denomO2) < 0.0001) denomO2 = 0.0001;
        rrf += (21.0 - 0.1 * (double)thermo.O2) / denomO2;

        rrf = (rrf * 3.5 + 0.6) * ((double)thermo.Tug - (double)thermo.Thw) * 0.01;
        eksut1 = 3.5 * (rrf - rrn) * wy;

        var denomTcb = tcb + 17.0;
        if (Math.Abs(denomTcb) < 0.0001) denomTcb = 0.0001;
        var rr = tcb + 0.07 * na + (20.0 + na) / denomTcb;
        eksut2 = 0.68 * wy * ((double)thermo.Tk - rr);

        eksut3 = 0.0;
        if ((double)thermo.Pop < 126.0)
        {
            if (na is >= 80.0 and <= 117.0)
                eksut3 = 0.148 * (na - 80.0) * wy;
            else if (na is > 117.0 and <= 160.0)
                eksut3 = 0.127 * (160.0 - na) * wy;
        }

        eksut4 = -0.07 * wy * ((double)thermo.Top - 540.0);

        var rrTpp = na switch
        {
            >= 70.0 and < 115.0 => 530.0 + 0.429 * (na - 80.0),
            >= 115.0 => 540.0,
            _ => 530.0,
        };
        eksut5 = (double)thermo.Tpp < 540.0
            ? -0.056 * wy * ((double)thermo.Tpp - rrTpp)
            : 0.0;

        var rrSn = (double)coeff.Kf1 * thermo.Hours + (double)coeff.Kf2 * wy + regimeAddend;
        eksut6 = (sn - rrSn) * 350.0;

        if (thermo.Pwd == 0)
            eksut7 = (2.98 + 0.0276 * (na - 80.0)) * wy;
        else
        {
            var rrTpw = (double)coeff.Kl2 + 0.3 * (na - 150.0);
            if (na < 150.0)
                rrTpw = 170.0 + (double)coeff.Kl1 * na;
            if (na >= 160.0)
                rrTpw = (double)coeff.Kl2 + 3.0 + 0.2 * (na - 150.0);

            eksut7 = -0.066 * ((double)thermo.Tpw - rrTpw + 0.05 * thermo.Dro) * wy;
        }
    }

    private static double Delta(double end, double start, double overflow)
    {
        var delta = end - start;
        return delta < 0 ? delta + overflow : delta;
    }

    private static double AverageNonZero(double[] values)
    {
        var nonZero = values.Where(v => v != 0).ToArray();
        return nonZero.Length == 0 ? 0 : FoxRound0(nonZero.Average());
    }

    /// <summary>karat ROUND(x, 0).</summary>
    private static double FoxRound0(double value) =>
        Math.Round(value, 0, MidpointRounding.AwayFromZero);

    /// <summary>karat SAY … pict "999.99".</summary>
    private static double RoundFoxPict99(double value) =>
        Math.Round(value, 2, MidpointRounding.ToEven);

    private static string FormatValue(BaxtaFuelRowKind kind, double value) =>
        kind == BaxtaFuelRowKind.Gkwh
            ? RoundFoxPict99(value).ToString("0.00", OptoCulture.Current)
            : FoxRound0(value).ToString("0", OptoCulture.Current);
}
