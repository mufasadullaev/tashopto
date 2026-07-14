using System.Collections.Generic;
using System.Linq;
using Opto.Models;

namespace Opto.Services;

public static class WyrabotkaInputValidator
{
    // Как в karat: pict 99 / 999999.9 / 999999.99 / 99999.99
    public const int MaxHours = 24;
    public const decimal MaxGenerationReading = 999_999.9m;
    public const decimal MaxOwnNeedsReading = 999_999.99m;
    public const decimal MaxTransformerReading = 99_999.99m;

    public static bool Validate(
        IEnumerable<BlockMeterRow> blocks,
        IEnumerable<TransformerMeterRow> transformers,
        out string error)
    {
        foreach (var block in blocks.OrderBy(b => b.Number))
        {
            if (block.Hours < 0 || block.Hours > MaxHours)
            {
                error = $"Блок {block.Number}: часы должны быть от 0 до {MaxHours}.";
                return false;
            }

            if (!IsValidReading(block.GenerationStart, MaxGenerationReading) ||
                !IsValidReading(block.GenerationEnd, MaxGenerationReading))
            {
                error = $"Блок {block.Number}: показания выработки — число от 0 до {MaxGenerationReading.ToString(OptoCulture.Current)} (точка).";
                return false;
            }

            if (!IsValidReading(block.OwnNeedsStart, MaxOwnNeedsReading) ||
                !IsValidReading(block.OwnNeedsEnd, MaxOwnNeedsReading))
            {
                error = $"Блок {block.Number}: показания СН — число от 0 до {MaxOwnNeedsReading.ToString(OptoCulture.Current)} (точка).";
                return false;
            }
        }

        foreach (var tr in transformers)
        {
            if (!IsValidReading(tr.Start, MaxTransformerReading) ||
                !IsValidReading(tr.End, MaxTransformerReading))
            {
                error = $"{tr.Name}: показания — число от 0 до {MaxTransformerReading.ToString(OptoCulture.Current)} (точка).";
                return false;
            }
        }

        error = "";
        return true;
    }

    private static bool IsValidReading(decimal value, decimal max) =>
        value >= 0 && value <= max;
}
