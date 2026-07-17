using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Opto.Models;

public sealed class PerejegEditSnapshot
{
    public PerejegRowData[] Rows { get; set; } = [];

    public static PerejegEditSnapshot FromRows(IEnumerable<PerejegInputRow> rows) =>
        new()
        {
            Rows = rows.Select(PerejegRowData.FromRow).ToArray(),
        };

    public void ApplyTo(IList<PerejegInputRow> rows)
    {
        foreach (var data in Rows)
        {
            var row = rows.FirstOrDefault(r => r.Index == data.Index);
            if (row is null)
                continue;

            data.ApplyTo(row);
        }
    }
}

public sealed class PerejegRowData
{
    public int Index { get; set; }
    public decimal Block1 { get; set; }
    public decimal Block2 { get; set; }
    public decimal Block3 { get; set; }
    public decimal Block4 { get; set; }
    public decimal Block5 { get; set; }
    public decimal Block6 { get; set; }
    public decimal Block7 { get; set; }
    public decimal Block8 { get; set; }
    public decimal Block9 { get; set; }
    public decimal Block10 { get; set; }
    public decimal Block11 { get; set; }
    public decimal Block12 { get; set; }
    public decimal Station { get; set; }

    public static PerejegRowData FromRow(PerejegInputRow row) => new()
    {
        Index = row.Index,
        Block1 = row.Block1,
        Block2 = row.Block2,
        Block3 = row.Block3,
        Block4 = row.Block4,
        Block5 = row.Block5,
        Block6 = row.Block6,
        Block7 = row.Block7,
        Block8 = row.Block8,
        Block9 = row.Block9,
        Block10 = row.Block10,
        Block11 = row.Block11,
        Block12 = row.Block12,
        Station = row.Station,
    };

    public void ApplyTo(PerejegInputRow row)
    {
        row.Block1 = Block1;
        row.Block2 = Block2;
        row.Block3 = Block3;
        row.Block4 = Block4;
        row.Block5 = Block5;
        row.Block6 = Block6;
        row.Block7 = Block7;
        row.Block8 = Block8;
        row.Block9 = Block9;
        row.Block10 = Block10;
        row.Block11 = Block11;
        row.Block12 = Block12;
        row.Station = Station;
    }

    public decimal GetBlock(int blockNumber) => blockNumber switch
    {
        1 => Block1,
        2 => Block2,
        3 => Block3,
        4 => Block4,
        5 => Block5,
        6 => Block6,
        7 => Block7,
        8 => Block8,
        9 => Block9,
        10 => Block10,
        11 => Block11,
        12 => Block12,
        _ => 0m,
    };
}

public sealed class PerejegResultData
{
    public decimal ReleaseMlnKwh { get; set; }
    public decimal TotalGkwh { get; set; }
    public decimal StationRouKg { get; set; }
    public decimal[][] DeviationKg { get; set; } = [];
    public decimal[] BlockTotalsKg { get; set; } = [];

    public string ToJson() => JsonSerializer.Serialize(this);

    public static PerejegResultData? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<PerejegResultData>(json);
    }
}
