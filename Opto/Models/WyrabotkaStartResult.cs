using System;

namespace Opto.Models;

public enum WyrabotkaMode
{
    Calculation,
    Recalculation,
}

public sealed class WyrabotkaStartResult
{
    public required DateTime Date { get; init; }
    public required WyrabotkaMode Mode { get; init; }
}
