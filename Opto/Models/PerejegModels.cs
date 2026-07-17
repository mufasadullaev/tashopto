using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Opto.Models;

public enum PerejegColumnScope
{
    BlocksOnly,
    StationOnly,
}

public sealed class PerejegRowDefinition
{
    public required int Index { get; init; }
    public required string Label { get; init; }
    public required PerejegColumnScope Scope { get; init; }
}

public static class PerejegDefinitions
{
    public static readonly PerejegRowDefinition[] InputRows =
    [
        new() { Index = 1, Label = "Nрвв, МВт", Scope = PerejegColumnScope.BlocksOnly },
        new() { Index = 2, Label = "N цн, МВт", Scope = PerejegColumnScope.BlocksOnly },
        new() { Index = 3, Label = "Nпвд, МВт", Scope = PerejegColumnScope.BlocksOnly },
        new() { Index = 4, Label = "Nснпнд, МВт", Scope = PerejegColumnScope.BlocksOnly },
        new() { Index = 5, Label = "N оэ, МВт", Scope = PerejegColumnScope.BlocksOnly },
        new() { Index = 6, Label = "Nсетев, МВт", Scope = PerejegColumnScope.StationOnly },
        new() { Index = 7, Label = "N авар, МВт", Scope = PerejegColumnScope.BlocksOnly },
        new() { Index = 8, Label = "Трвв, час", Scope = PerejegColumnScope.BlocksOnly },
        new() { Index = 9, Label = "Т цн, час", Scope = PerejegColumnScope.BlocksOnly },
        new() { Index = 10, Label = "Тпвд, час", Scope = PerejegColumnScope.BlocksOnly },
        new() { Index = 11, Label = "Тснпнд, час", Scope = PerejegColumnScope.BlocksOnly },
        new() { Index = 12, Label = "Т оэ, час", Scope = PerejegColumnScope.BlocksOnly },
        new() { Index = 13, Label = "Тсетев, час", Scope = PerejegColumnScope.StationOnly },
        new() { Index = 14, Label = "Т авар, час", Scope = PerejegColumnScope.BlocksOnly },
        new() { Index = 15, Label = "DTавар, грд", Scope = PerejegColumnScope.BlocksOnly },
        new() { Index = 16, Label = "Эj, млн кВт·ч", Scope = PerejegColumnScope.StationOnly },
    ];

    public static readonly string[] DeviationLabels =
    [
        "Отключение 1 РВВ",
        "Работа 1 Ц",
        "Отключение группы ПВД",
        "Работа без обоих СПД",
        "Включение второго ОЭ",
        "Перев. сет. подогр. на РОУ",
        "Работа авар. впр",
        "Итого, кг у.т.",
        "Итого, г/кВт·ч",
    ];

    public static readonly decimal[] DeviationCoefficients = [10.6m, 7.1m, 6.0m, 1.9m, 0.7m, 20.0m, 444.0m];
}

public partial class PerejegInputRow : ObservableObject
{
    public int Index { get; init; }
    public string Label { get; init; } = "";
    public PerejegColumnScope Scope { get; init; }
    public bool UsesBlocks => Scope == PerejegColumnScope.BlocksOnly;
    public bool UsesStation => Scope == PerejegColumnScope.StationOnly;

    [ObservableProperty] private decimal _block1;
    [ObservableProperty] private decimal _block2;
    [ObservableProperty] private decimal _block3;
    [ObservableProperty] private decimal _block4;
    [ObservableProperty] private decimal _block5;
    [ObservableProperty] private decimal _block6;
    [ObservableProperty] private decimal _block7;
    [ObservableProperty] private decimal _block8;
    [ObservableProperty] private decimal _block9;
    [ObservableProperty] private decimal _block10;
    [ObservableProperty] private decimal _block11;
    [ObservableProperty] private decimal _block12;
    [ObservableProperty] private decimal _station;

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

    public void SetBlock(int blockNumber, decimal value)
    {
        switch (blockNumber)
        {
            case 1: Block1 = value; break;
            case 2: Block2 = value; break;
            case 3: Block3 = value; break;
            case 4: Block4 = value; break;
            case 5: Block5 = value; break;
            case 6: Block6 = value; break;
            case 7: Block7 = value; break;
            case 8: Block8 = value; break;
            case 9: Block9 = value; break;
            case 10: Block10 = value; break;
            case 11: Block11 = value; break;
            case 12: Block12 = value; break;
        }
    }

    public decimal[] BlockValues =>
    [
        Block1, Block2, Block3, Block4, Block5, Block6,
        Block7, Block8, Block9, Block10, Block11, Block12,
    ];
}

public sealed class PerejegReportRow
{
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
    public required string StationText { get; init; }
    public bool IsSummary { get; init; }
}

public sealed class PerejegReport
{
    public required DateTime Date { get; init; }
    public required WyrabotkaMode Mode { get; init; }
    public required IReadOnlyList<PerejegReportRow> Rows { get; init; }
    public required decimal ReleaseMlnKwh { get; init; }
    public required decimal TotalGkwh { get; init; }
    public bool IsCumulative { get; init; }
    public DateTime? PeriodFrom { get; init; }
    public DateTime? PeriodTo { get; init; }
    public bool DisplayInTons { get; init; }
}
