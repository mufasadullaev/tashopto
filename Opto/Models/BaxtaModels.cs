using CommunityToolkit.Mvvm.ComponentModel;

namespace Opto.Models;

public partial class BaxtaMeterBlockRow : ObservableObject
{
    public int Number { get; init; }

    [ObservableProperty]
    private decimal _generationCoefficient;

    [ObservableProperty]
    private decimal _generationAt0;

    [ObservableProperty]
    private decimal _generationAt8;

    [ObservableProperty]
    private decimal _generationAt16;

    [ObservableProperty]
    private decimal _generationAt24;

    [ObservableProperty]
    private decimal _ownNeedsCoefficient;

    [ObservableProperty]
    private decimal _ownNeedsAt0;

    [ObservableProperty]
    private decimal _ownNeedsAt8;

    [ObservableProperty]
    private decimal _ownNeedsAt16;

    [ObservableProperty]
    private decimal _ownNeedsAt24;
}

public partial class BaxtaBlockCoeffsRow : ObservableObject
{
    public int Number { get; init; }

    [ObservableProperty]
    private decimal _kl1;

    [ObservableProperty]
    private decimal _kl2;

    [ObservableProperty]
    private decimal _kf1;

    [ObservableProperty]
    private decimal _kf2;

    [ObservableProperty]
    private decimal _kf3;

    [ObservableProperty]
    private decimal _kf4;
}

public partial class BaxtaThermoRow : ObservableObject
{
    public int BlockNumber { get; init; }
    public int ShiftIndex { get; init; }
    public string ShiftLabel { get; init; } = "";

    [ObservableProperty]
    private int _hours = 8;

    [ObservableProperty]
    private int _dro;

    [ObservableProperty]
    private int _pwd;

    [ObservableProperty]
    private decimal _tpw;

    [ObservableProperty]
    private decimal _tk = 540m;

    [ObservableProperty]
    private decimal _top = 540m;

    [ObservableProperty]
    private decimal _tpp = 540m;

    [ObservableProperty]
    private decimal _pop = 120m;

    [ObservableProperty]
    private decimal _tug = 15m;

    [ObservableProperty]
    private decimal _thw = 15m;

    [ObservableProperty]
    private decimal _o2 = 3.5m;

    [ObservableProperty]
    private int _tn;
}

public partial class BaxtaPlantParamsRow : ObservableObject
{
    [ObservableProperty]
    private decimal _urp = 324.90m;

    [ObservableProperty]
    private decimal _urm = 452.40m;

    [ObservableProperty]
    private decimal _tcb1 = 10.8m;

    [ObservableProperty]
    private decimal _tcb2 = 11.0m;

    [ObservableProperty]
    private decimal _tcb3 = 11.5m;

    [ObservableProperty]
    private int _wrmn11;

    [ObservableProperty]
    private int _wrmn12;

    [ObservableProperty]
    private int _wrmn13;

    [ObservableProperty]
    private int _wrmn21;

    [ObservableProperty]
    private int _wrmn22;

    [ObservableProperty]
    private int _wrmn23;

    [ObservableProperty]
    private int _wrmn31;

    [ObservableProperty]
    private int _wrmn32;

    [ObservableProperty]
    private int _wrmn33;

    [ObservableProperty]
    private int _wrmn14;

    [ObservableProperty]
    private int _wrmn24;

    [ObservableProperty]
    private int _wrmn34;

    [ObservableProperty]
    private int _setn1K;

    [ObservableProperty]
    private int _setn1N;

    [ObservableProperty]
    private int _setn2K;

    [ObservableProperty]
    private int _setn2N;

    [ObservableProperty]
    private int _setn3K;

    [ObservableProperty]
    private int _setn3N;

    [ObservableProperty]
    private int _setn4K;

    [ObservableProperty]
    private int _setn4N;

    [ObservableProperty]
    private int _setn5K;

    [ObservableProperty]
    private int _setn5N;

    public int[] Prises { get; } = [43, 56, 53, 58, 53, 53, 46, 53, 52, 52, 49, 46];
}

public partial class BaxtaPrisRow : ObservableObject
{
    public int BlockNumber { get; init; }

    [ObservableProperty]
    private int _value = 43;
}
