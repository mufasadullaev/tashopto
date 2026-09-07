using System;
using System.Collections.Generic;
using System.Linq;
using Opto.Models;

namespace Opto.Models;

public sealed class WyrabotkaEditSnapshot
{
    public WyrabotkaBlockData[] Blocks { get; set; } = [];
    public WyrabotkaTransformerData[] Transformers { get; set; } = [];

    public static WyrabotkaEditSnapshot FromRows(
        IEnumerable<BlockMeterRow> blocks,
        IEnumerable<TransformerMeterRow> transformers) =>
        new()
        {
            Blocks = blocks.Select(b => new WyrabotkaBlockData
            {
                Number = b.Number,
                GenerationCoefficient = b.GenerationCoefficient,
                GenerationStart = b.GenerationStart,
                GenerationEnd = b.GenerationEnd,
                OwnNeedsCoefficient = b.OwnNeedsCoefficient,
                OwnNeedsStart = b.OwnNeedsStart,
                OwnNeedsEnd = b.OwnNeedsEnd,
                Hours = b.Hours,
            }).ToArray(),
            Transformers = transformers.Select(t => new WyrabotkaTransformerData
            {
                Name = t.Name,
                Coefficient = t.Coefficient,
                Start = t.Start,
                End = t.End,
            }).ToArray(),
        };

    public void ApplyTo(
        IList<BlockMeterRow> blocks,
        IList<TransformerMeterRow> transformers)
    {
        foreach (var item in Blocks)
        {
            var row = blocks.FirstOrDefault(b => b.Number == item.Number);
            if (row is null)
                continue;

            row.GenerationCoefficient = item.GenerationCoefficient;
            row.GenerationStart = item.GenerationStart;
            row.GenerationEnd = item.GenerationEnd;
            row.OwnNeedsCoefficient = item.OwnNeedsCoefficient;
            row.OwnNeedsStart = item.OwnNeedsStart;
            row.OwnNeedsEnd = item.OwnNeedsEnd;
            row.Hours = item.Hours;
        }

        foreach (var item in Transformers)
        {
            var row = transformers.FirstOrDefault(t => t.Name == item.Name);
            if (row is null)
                continue;

            row.Coefficient = item.Coefficient;
            row.Start = item.Start;
            row.End = item.End;
        }
    }

    public List<BlockMeterRow> ToBlockRows() =>
        Blocks.Select(b => new BlockMeterRow
        {
            Number = b.Number,
            GenerationCoefficient = b.GenerationCoefficient,
            GenerationStart = b.GenerationStart,
            GenerationEnd = b.GenerationEnd,
            OwnNeedsCoefficient = b.OwnNeedsCoefficient,
            OwnNeedsStart = b.OwnNeedsStart,
            OwnNeedsEnd = b.OwnNeedsEnd,
            Hours = b.Hours,
        }).ToList();

    public List<TransformerMeterRow> ToTransformerRows() =>
        Transformers.Select(t => new TransformerMeterRow
        {
            Name = t.Name,
            Coefficient = t.Coefficient,
            Start = t.Start,
            End = t.End,
        }).ToList();
}

public sealed class WyrabotkaBlockData
{
    public int Number { get; set; }
    public decimal GenerationCoefficient { get; set; }
    public decimal GenerationStart { get; set; }
    public decimal GenerationEnd { get; set; }
    public decimal OwnNeedsCoefficient { get; set; }
    public decimal OwnNeedsStart { get; set; }
    public decimal OwnNeedsEnd { get; set; }
    public int Hours { get; set; }
}

public sealed class WyrabotkaTransformerData
{
    public string Name { get; set; } = "";
    public decimal Coefficient { get; set; }
    public decimal Start { get; set; }
    public decimal End { get; set; }
}

public sealed class WyrabotkaMonthTotals
{
    public int Hours { get; init; }
    public decimal GenerationThousandKwh { get; init; }
    public decimal OwnNeedsThousandKwh { get; init; }
    public decimal ReleaseThousandKwh { get; init; }
}
