using System;
using System.Collections.Generic;

namespace Opto.Models;

public sealed class AktBlockRow
{
    public required int BlockNumber { get; init; }
    public required decimal GenerationKwh { get; init; }
    public required decimal OwnNeedsKwh { get; init; }
    public required decimal CompensatorKwh { get; init; }
}

public sealed class AktIntersystemFlow
{
    public required string Name { get; init; }
    public required decimal MeterStart { get; init; }
    public required decimal MeterEnd { get; init; }
    public required decimal Coefficient { get; init; }
    public decimal ValueKwh => Coefficient * Math.Max(0, MeterEnd - MeterStart);
}

public sealed class AktInputSnapshot
{
    public DateTime Date { get; set; } = DateTime.Today;
    public decimal ReserveExciterKwh { get; set; }
    public decimal LossesKwh { get; set; }
    public decimal PlantFacilitiesKwh { get; set; }
    public decimal PreventoriumKwh { get; set; }
    public List<AktIntersystemFlow> IntakeFlows { get; set; } = [];
    public List<AktIntersystemFlow> DeliveryFlows { get; set; } = [];
}

public sealed class AktReport
{
    public required DateTime Date { get; init; }
    public required IReadOnlyList<AktBlockRow> Blocks { get; init; }
    public required decimal ReserveExciterKwh { get; init; }
    public required decimal LossesKwh { get; init; }
    public required decimal PlantFacilitiesKwh { get; init; }
    public required decimal PreventoriumKwh { get; init; }
    public required decimal BlockOwnNeedsTotalKwh { get; init; }
    public required decimal TransformerOwnNeedsTotalKwh { get; init; }
    public required decimal TotalOwnNeedsKwh { get; init; }
    public required decimal GrossGenerationKwh { get; init; }
    public required decimal NetReleaseKwh { get; init; }
    public required IReadOnlyList<AktIntersystemFlow> IntakeFlows { get; init; }
    public required IReadOnlyList<AktIntersystemFlow> DeliveryFlows { get; init; }
    public required decimal TotalIntakeKwh { get; init; }
    public required decimal TotalDeliveryKwh { get; init; }
}
