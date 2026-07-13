using CommunityToolkit.Mvvm.ComponentModel;

namespace Opto.Models;

public partial class BlockMeterRow : ObservableObject
{
    public int Number { get; init; }

    [ObservableProperty]
    private decimal _generationCoefficient;

    [ObservableProperty]
    private decimal _generationStart;

    [ObservableProperty]
    private decimal _generationEnd;

    [ObservableProperty]
    private decimal _ownNeedsCoefficient;

    [ObservableProperty]
    private decimal _ownNeedsStart;

    [ObservableProperty]
    private decimal _ownNeedsEnd;

    [ObservableProperty]
    private int _hours;
}

public partial class TransformerMeterRow : ObservableObject
{
    public required string Name { get; init; }

    [ObservableProperty]
    private decimal _coefficient;

    [ObservableProperty]
    private decimal _start;

    [ObservableProperty]
    private decimal _end;
}
