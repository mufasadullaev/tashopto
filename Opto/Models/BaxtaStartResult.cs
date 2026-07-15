using System;

namespace Opto.Models;

public sealed class BaxtaStartResult
{
    public required DateTime Date { get; init; }
    public required WyrabotkaMode Mode { get; init; }
}
