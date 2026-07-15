using System.Collections.Generic;
using System.Linq;
using Opto.Models;

namespace Opto.Services;

public static class BaxtaInputValidator
{
    public const int MaxHours = 8;
    public const decimal MaxGenerationReading = 999_999.9m;
    public const decimal MaxOwnNeedsReading = 9_999.99m;

    public static bool Validate(
        IEnumerable<BaxtaMeterBlockRow> meters,
        BaxtaPlantParamsRow plant,
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
                error = $"Блок {t.BlockNumber}, {t.ShiftLabel}: часы 0…{MaxHours}.";
                return false;
            }

            if (t.Pwd is not (0 or 1))
            {
                error = $"Блок {t.BlockNumber}, {t.ShiftLabel}: ПВД — 0 или 1.";
                return false;
            }
        }

        if (plant.Urp < 0 || plant.Urm < 0)
        {
            error = "Удельный расход топлива не может быть отрицательным.";
            return false;
        }

        if (!ValidatePlantPumpHours(plant, out error))
            return false;

        foreach (var pris in plant.Prises)
        {
            if (pris < 0 || pris > 99)
            {
                error = "Присоски: значение от 0 до 99.";
                return false;
            }
        }

        if (plant.Tcb1 is < 0 or > 99.9m || plant.Tcb2 is < 0 or > 99.9m || plant.Tcb3 is < 0 or > 99.9m)
        {
            error = "Температура циркулирующей воды: от 0 до 99.9.";
            return false;
        }

        error = "";
        return true;
    }

    private static bool OkPumpHours(int value) => value is >= 0 and <= 9;

    public static bool ValidatePlantPumpHours(BaxtaPlantParamsRow plant, out string error)
    {
        var fields = new (string Name, int Value)[]
        {
            ("N4 смена 1", plant.Wrmn11), ("N5 смена 1", plant.Wrmn21), ("N6 смена 1", plant.Wrmn31), ("N7 смена 1", plant.Wrmn14),
            ("N4 смена 2", plant.Wrmn12), ("N5 смена 2", plant.Wrmn22), ("N6 смена 2", plant.Wrmn32), ("N7 смена 2", plant.Wrmn24),
            ("N4 смена 3", plant.Wrmn13), ("N5 смена 3", plant.Wrmn23), ("N6 смена 3", plant.Wrmn33), ("N7 смена 3", plant.Wrmn34),
        };

        foreach (var (name, value) in fields)
        {
            if (!OkPumpHours(value))
            {
                error = $"Мазутные насосы ({name}): часы 0…9.";
                return false;
            }
        }

        error = "";
        return true;
    }

    private static bool Ok(decimal value, decimal max) => value >= 0 && value <= max;
}
