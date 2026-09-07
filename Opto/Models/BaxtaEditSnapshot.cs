using System;
using System.Collections.Generic;
using System.Linq;
using Opto.Models;

namespace Opto.Models;

public sealed class BaxtaEditSnapshot
{
    public BaxtaMeterData[] Meters { get; set; } = [];
    public BaxtaCoeffsData[] Coeffs { get; set; } = [];
    public BaxtaThermoData[] Thermo { get; set; } = [];
    public BaxtaPlantData Plant { get; set; } = new();

    public static BaxtaEditSnapshot FromRows(
        IEnumerable<BaxtaMeterBlockRow> meters,
        IEnumerable<BaxtaBlockCoeffsRow> coeffs,
        IEnumerable<BaxtaThermoRow> thermo,
        BaxtaPlantParamsRow plant) =>
        new()
        {
            Meters = meters.Select(m => new BaxtaMeterData
            {
                Number = m.Number,
                GenerationCoefficient = m.GenerationCoefficient,
                GenerationAt0 = m.GenerationAt0,
                GenerationAt8 = m.GenerationAt8,
                GenerationAt16 = m.GenerationAt16,
                GenerationAt24 = m.GenerationAt24,
                OwnNeedsCoefficient = m.OwnNeedsCoefficient,
                OwnNeedsAt0 = m.OwnNeedsAt0,
                OwnNeedsAt8 = m.OwnNeedsAt8,
                OwnNeedsAt16 = m.OwnNeedsAt16,
                OwnNeedsAt24 = m.OwnNeedsAt24,
            }).ToArray(),
            Coeffs = coeffs.Select(c => new BaxtaCoeffsData
            {
                Number = c.Number,
                Kl1 = c.Kl1,
                Kl2 = c.Kl2,
                Kf1 = c.Kf1,
                Kf2 = c.Kf2,
                Kf3 = c.Kf3,
                Kf4 = c.Kf4,
            }).ToArray(),
            Thermo = thermo.Select(t => new BaxtaThermoData
            {
                BlockNumber = t.BlockNumber,
                ShiftIndex = t.ShiftIndex,
                ShiftLabel = t.ShiftLabel,
                Hours = t.Hours,
                Dro = t.Dro,
                Pwd = t.Pwd,
                Tpw = t.Tpw,
                Tk = t.Tk,
                Top = t.Top,
                Tpp = t.Tpp,
                Pop = t.Pop,
                Tug = t.Tug,
                Thw = t.Thw,
                O2 = t.O2,
                Tn = t.Tn,
            }).ToArray(),
            Plant = BaxtaPlantData.FromRow(plant),
        };

    public void ApplyTo(
        IList<BaxtaMeterBlockRow> meters,
        IList<BaxtaBlockCoeffsRow> coeffs,
        IList<BaxtaThermoRow> thermo,
        BaxtaPlantParamsRow plant)
    {
        foreach (var item in Meters)
        {
            var row = meters.FirstOrDefault(m => m.Number == item.Number);
            if (row is null)
                continue;

            row.GenerationCoefficient = item.GenerationCoefficient;
            row.GenerationAt0 = item.GenerationAt0;
            row.GenerationAt8 = item.GenerationAt8;
            row.GenerationAt16 = item.GenerationAt16;
            row.GenerationAt24 = item.GenerationAt24;
            row.OwnNeedsCoefficient = item.OwnNeedsCoefficient;
            row.OwnNeedsAt0 = item.OwnNeedsAt0;
            row.OwnNeedsAt8 = item.OwnNeedsAt8;
            row.OwnNeedsAt16 = item.OwnNeedsAt16;
            row.OwnNeedsAt24 = item.OwnNeedsAt24;
        }

        foreach (var item in Coeffs)
        {
            var row = coeffs.FirstOrDefault(c => c.Number == item.Number);
            if (row is null)
                continue;

            row.Kl1 = item.Kl1;
            row.Kl2 = item.Kl2;
            row.Kf1 = item.Kf1;
            row.Kf2 = item.Kf2;
            row.Kf3 = item.Kf3;
            row.Kf4 = item.Kf4;
        }

        foreach (var item in Thermo)
        {
            var row = thermo.FirstOrDefault(t =>
                t.BlockNumber == item.BlockNumber && t.ShiftIndex == item.ShiftIndex);
            if (row is null)
                continue;

            row.Hours = item.Hours;
            row.Dro = item.Dro;
            row.Pwd = item.Pwd;
            row.Tpw = item.Tpw;
            row.Tk = item.Tk;
            row.Top = item.Top;
            row.Tpp = item.Tpp;
            row.Pop = item.Pop;
            row.Tug = item.Tug;
            row.Thw = item.Thw;
            row.O2 = item.O2;
            row.Tn = item.Tn;
        }

        Plant.ApplyTo(plant);
    }
}

public sealed class BaxtaMeterData
{
    public int Number { get; set; }
    public decimal GenerationCoefficient { get; set; }
    public decimal GenerationAt0 { get; set; }
    public decimal GenerationAt8 { get; set; }
    public decimal GenerationAt16 { get; set; }
    public decimal GenerationAt24 { get; set; }
    public decimal OwnNeedsCoefficient { get; set; }
    public decimal OwnNeedsAt0 { get; set; }
    public decimal OwnNeedsAt8 { get; set; }
    public decimal OwnNeedsAt16 { get; set; }
    public decimal OwnNeedsAt24 { get; set; }
}

public sealed class BaxtaCoeffsData
{
    public int Number { get; set; }
    public decimal Kl1 { get; set; }
    public decimal Kl2 { get; set; }
    public decimal Kf1 { get; set; }
    public decimal Kf2 { get; set; }
    public decimal Kf3 { get; set; }
    public decimal Kf4 { get; set; }
}

public sealed class BaxtaThermoData
{
    public int BlockNumber { get; set; }
    public int ShiftIndex { get; set; }
    public string ShiftLabel { get; set; } = "";
    public int Hours { get; set; }
    public int Dro { get; set; }
    public int Pwd { get; set; }
    public decimal Tpw { get; set; }
    public decimal Tk { get; set; }
    public decimal Top { get; set; }
    public decimal Tpp { get; set; }
    public decimal Pop { get; set; }
    public decimal Tug { get; set; }
    public decimal Thw { get; set; }
    public decimal O2 { get; set; }
    public int Tn { get; set; }
}

public sealed class BaxtaPlantData
{
    public decimal Urp { get; set; }
    public decimal Urm { get; set; }
    public decimal Tcb1 { get; set; }
    public decimal Tcb2 { get; set; }
    public decimal Tcb3 { get; set; }
    public int Wrmn11 { get; set; }
    public int Wrmn12 { get; set; }
    public int Wrmn13 { get; set; }
    public int Wrmn21 { get; set; }
    public int Wrmn22 { get; set; }
    public int Wrmn23 { get; set; }
    public int Wrmn31 { get; set; }
    public int Wrmn32 { get; set; }
    public int Wrmn33 { get; set; }
    public int Wrmn14 { get; set; }
    public int Wrmn24 { get; set; }
    public int Wrmn34 { get; set; }
    public int Setn1K { get; set; }
    public int Setn1N { get; set; }
    public int Setn2K { get; set; }
    public int Setn2N { get; set; }
    public int Setn3K { get; set; }
    public int Setn3N { get; set; }
    public int Setn4K { get; set; }
    public int Setn4N { get; set; }
    public int Setn5K { get; set; }
    public int Setn5N { get; set; }
    public int[] Prises { get; set; } = new int[12];

    public static BaxtaPlantData FromRow(BaxtaPlantParamsRow plant) =>
        new()
        {
            Urp = plant.Urp,
            Urm = plant.Urm,
            Tcb1 = plant.Tcb1,
            Tcb2 = plant.Tcb2,
            Tcb3 = plant.Tcb3,
            Wrmn11 = plant.Wrmn11,
            Wrmn12 = plant.Wrmn12,
            Wrmn13 = plant.Wrmn13,
            Wrmn21 = plant.Wrmn21,
            Wrmn22 = plant.Wrmn22,
            Wrmn23 = plant.Wrmn23,
            Wrmn31 = plant.Wrmn31,
            Wrmn32 = plant.Wrmn32,
            Wrmn33 = plant.Wrmn33,
            Wrmn14 = plant.Wrmn14,
            Wrmn24 = plant.Wrmn24,
            Wrmn34 = plant.Wrmn34,
            Setn1K = plant.Setn1K,
            Setn1N = plant.Setn1N,
            Setn2K = plant.Setn2K,
            Setn2N = plant.Setn2N,
            Setn3K = plant.Setn3K,
            Setn3N = plant.Setn3N,
            Setn4K = plant.Setn4K,
            Setn4N = plant.Setn4N,
            Setn5K = plant.Setn5K,
            Setn5N = plant.Setn5N,
            Prises = plant.Prises.ToArray(),
        };

    public void ApplyTo(BaxtaPlantParamsRow plant)
    {
        plant.Urp = Urp;
        plant.Urm = Urm;
        plant.Tcb1 = Tcb1;
        plant.Tcb2 = Tcb2;
        plant.Tcb3 = Tcb3;
        plant.Wrmn11 = Wrmn11;
        plant.Wrmn12 = Wrmn12;
        plant.Wrmn13 = Wrmn13;
        plant.Wrmn21 = Wrmn21;
        plant.Wrmn22 = Wrmn22;
        plant.Wrmn23 = Wrmn23;
        plant.Wrmn31 = Wrmn31;
        plant.Wrmn32 = Wrmn32;
        plant.Wrmn33 = Wrmn33;
        plant.Wrmn14 = Wrmn14;
        plant.Wrmn24 = Wrmn24;
        plant.Wrmn34 = Wrmn34;
        plant.Setn1K = Setn1K;
        plant.Setn1N = Setn1N;
        plant.Setn2K = Setn2K;
        plant.Setn2N = Setn2N;
        plant.Setn3K = Setn3K;
        plant.Setn3N = Setn3N;
        plant.Setn4K = Setn4K;
        plant.Setn4N = Setn4N;
        plant.Setn5K = Setn5K;
        plant.Setn5N = Setn5N;

        for (var i = 0; i < Math.Min(Prises.Length, plant.Prises.Length); i++)
            plant.Prises[i] = Prises[i];
    }
}

public static class BaxtaEditSnapshotExtensions
{
    public static List<BaxtaMeterBlockRow> ToMeterRows(this BaxtaEditSnapshot snapshot) =>
        snapshot.Meters.Select(m => new BaxtaMeterBlockRow
        {
            Number = m.Number,
            GenerationCoefficient = m.GenerationCoefficient,
            GenerationAt0 = m.GenerationAt0,
            GenerationAt8 = m.GenerationAt8,
            GenerationAt16 = m.GenerationAt16,
            GenerationAt24 = m.GenerationAt24,
            OwnNeedsCoefficient = m.OwnNeedsCoefficient,
            OwnNeedsAt0 = m.OwnNeedsAt0,
            OwnNeedsAt8 = m.OwnNeedsAt8,
            OwnNeedsAt16 = m.OwnNeedsAt16,
            OwnNeedsAt24 = m.OwnNeedsAt24,
        }).ToList();

    public static List<BaxtaBlockCoeffsRow> ToCoeffRows(this BaxtaEditSnapshot snapshot) =>
        snapshot.Coeffs.Select(c => new BaxtaBlockCoeffsRow
        {
            Number = c.Number,
            Kl1 = c.Kl1,
            Kl2 = c.Kl2,
            Kf1 = c.Kf1,
            Kf2 = c.Kf2,
            Kf3 = c.Kf3,
            Kf4 = c.Kf4,
        }).ToList();

    public static List<BaxtaThermoRow> ToThermoRows(this BaxtaEditSnapshot snapshot) =>
        snapshot.Thermo.Select(t => new BaxtaThermoRow
        {
            BlockNumber = t.BlockNumber,
            ShiftIndex = t.ShiftIndex,
            ShiftLabel = t.ShiftLabel,
            Hours = t.Hours,
            Dro = t.Dro,
            Pwd = t.Pwd,
            Tpw = t.Tpw,
            Tk = t.Tk,
            Top = t.Top,
            Tpp = t.Tpp,
            Pop = t.Pop,
            Tug = t.Tug,
            Thw = t.Thw,
            O2 = t.O2,
            Tn = t.Tn,
        }).ToList();

    public static BaxtaPlantParamsRow ToPlantRow(this BaxtaEditSnapshot snapshot)
    {
        var plant = new BaxtaPlantParamsRow();
        snapshot.Plant.ApplyTo(plant);
        return plant;
    }
}
