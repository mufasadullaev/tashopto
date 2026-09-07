using System;
using System.Collections.Generic;

namespace Opto.Models;

public sealed class O2GolSummaryRow
{
    public required string Label { get; init; }
    public required IReadOnlyList<string> Values { get; init; }
    public required string TotalText { get; init; }
}

public sealed class O2GolSummarySection
{
    public required string Title { get; init; }
    public required IReadOnlyList<string> ColumnHeaders { get; init; }
    public required IReadOnlyList<O2GolSummaryRow> Rows { get; init; }
}

public sealed class O2GolOperatorRow
{
    public required int OperatorTn { get; init; }
    public required int BlockNumber { get; init; }
    public required int ShiftCount { get; init; }
    public required string GenerationText { get; init; }
    public required string LoadText { get; init; }
    public required string PugText { get; init; }
    public required string WakText { get; init; }
    public required string DopText { get; init; }
    public required string TopText { get; init; }
    public required string TppText { get; init; }
    public required string SnText { get; init; }
    public required string TpwText { get; init; }
    public required string TotalText { get; init; }
    public required string GkwhText { get; init; }
}

public sealed class O2GolOperatorGroup
{
    public required string Title { get; init; }
    public required IReadOnlyList<O2GolOperatorRow> Rows { get; init; }
}

public sealed class O2GolReport
{
    public required DateTime From { get; init; }
    public required DateTime To { get; init; }
    public required O2GolSummarySection ByBlocks { get; init; }
    public required O2GolSummarySection ByWatches { get; init; }
    public required IReadOnlyList<O2GolOperatorGroup> OperatorGroups { get; init; }
}
