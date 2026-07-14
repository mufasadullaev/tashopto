namespace Opto.Models;

public class BaxtaMeterBlockRow
{
    public int Number { get; init; }

    public decimal GenerationAt0 { get; init; }
    public decimal GenerationAt8 { get; init; }
    public decimal GenerationAt16 { get; init; }
    public decimal GenerationAt24 { get; init; }

    public decimal OwnNeedsAt0 { get; init; }
    public decimal OwnNeedsAt8 { get; init; }
    public decimal OwnNeedsAt16 { get; init; }
    public decimal OwnNeedsAt24 { get; init; }
}

public class BaxtaThermoRow
{
    public int BlockNumber { get; init; }
    public string ShiftLabel { get; init; } = "";
    public int Hours { get; init; }
    public int Pwd { get; init; }
}

public class BaxtaPlantParams
{
    public decimal Urp { get; init; }
    public decimal Urm { get; init; }
}
