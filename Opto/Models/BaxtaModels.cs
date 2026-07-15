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
    private int _hours;

    [ObservableProperty]
    private int _dro;

    [ObservableProperty]
    private int _pwd;

    [ObservableProperty]
    private decimal _tpw;

    [ObservableProperty]
    private decimal _tk;

    [ObservableProperty]
    private decimal _top;

    [ObservableProperty]
    private decimal _tpp;

    [ObservableProperty]
    private decimal _pop;

    [ObservableProperty]
    private decimal _tug;

    [ObservableProperty]
    private decimal _thw;

    [ObservableProperty]
    private decimal _o2;

    [ObservableProperty]
    private int _tn;
}

public partial class BaxtaPlantParamsRow : ObservableObject
{
    [ObservableProperty]
    private decimal _urp;

    [ObservableProperty]
    private decimal _urm;

    [ObservableProperty]
    private decimal _tcb1;

    [ObservableProperty]
    private decimal _tcb2;

    [ObservableProperty]
    private decimal _tcb3;

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

    public int[] Prises { get; } = new int[12];
}

public partial class BaxtaPrisRow : ObservableObject
{
    public int BlockNumber { get; init; }

    [ObservableProperty]
    private int _value;
}
