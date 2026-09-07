using System;
using System.Collections.Generic;

namespace Opto.Models;

public sealed class SelektorBlockStatus
{
    public required int BlockNumber { get; init; }
    public required decimal GenerationMw { get; init; }
    public required decimal OwnNeedsMw { get; init; }
    public required int OperatingHours { get; init; }
    public required string Comment { get; init; }
}

public sealed class SelektorReport
{
    public required DateTime Date { get; init; }
    public required IReadOnlyList<SelektorBlockStatus> Blocks { get; init; }
    public required decimal TotalGenerationMw { get; init; }
    public required decimal TotalOwnNeedsMw { get; init; }
    public required decimal StationReleaseMw { get; init; }
}
