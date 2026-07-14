using System.Collections.Generic;
using System.Linq;
using Opto.Models;

namespace Opto.Services;

public static class BaxtaInputValidator
{
    public const int MaxHours = 8;
    public const decimal MaxGenerationReading = 999_999.9m;
    public const decimal MaxOwnNeedsReading = 999_999.99m;

    public static bool Validate(
        IEnumerable<BaxtaMeterBlockRow> meters,
        BaxtaPlantParams plant,
        IEnumerable<BaxtaThermoRow> thermo,
        out string error)
    {
        foreach (var m in meters.OrderBy(x => x.Number))
        {
            if (!Ok(m.GenerationAt0, MaxGenerationReading) ||
                !Ok(m.GenerationAt8, MaxGenerationReading) ||
                !Ok(m.GenerationAt16, MaxGenerationReading) ||
                !Ok(m.GenerationAt24, MaxGenerationReading))
            {
                error = $"Блок {m.Number}: показания выработки — от 0 до {MaxGenerationReading.ToString(OptoCulture.Current)}.";
                return false;
            }

            if (!Ok(m.OwnNeedsAt0, MaxOwnNeedsReading) ||
                !Ok(m.OwnNeedsAt8, MaxOwnNeedsReading) ||
                !Ok(m.OwnNeedsAt16, MaxOwnNeedsReading) ||
                !Ok(m.OwnNeedsAt24, MaxOwnNeedsReading))
            {
                error = $"Блок {m.Number}: показания СН — от 0 до {MaxOwnNeedsReading.ToString(OptoCulture.Current)}.";
                return false;
            }
        }

        foreach (var t in thermo)
        {
            if (t.Hours < 0 || t.Hours > MaxHours)
            {
                error = $"Блок {t.BlockNumber}, смена {t.ShiftLabel}: часы 0…{MaxHours}.";
                return false;
            }

            if (t.Pwd is not (0 or 1))
            {
                error = $"Блок {t.BlockNumber}, смена {t.ShiftLabel}: ПВД — 0 или 1.";
                return false;
            }
        }

        if (plant.Urp < 0 || plant.Urm < 0)
        {
            error = "Удельный расход топлива не может быть отрицательным.";
            return false;
        }

        error = "";
        return true;
    }

    private static bool Ok(decimal value, decimal max) => value >= 0 && value <= max;
}
