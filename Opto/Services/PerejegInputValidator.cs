using System.Collections.Generic;

namespace Opto.Services;

using Opto.Models;

public static class PerejegInputValidator
{
    private const decimal MaxValue = 999_999.999m;

    public static bool Validate(IReadOnlyList<PerejegInputRow> rows, out string? error)
    {
        error = null;

        if (rows.Count != 16)
        {
            error = "Неверная структура данных пережогов.";
            return false;
        }

        foreach (var row in rows)
        {
            if (!ValidateRowValues(row, out error))
                return false;
        }

        if (!ValidatePairedPowerTime(rows, 1, 8, out error))
            return false;

        if (!ValidatePairedPowerTime(rows, 2, 9, out error))
            return false;

        if (!ValidatePairedPowerTime(rows, 3, 10, out error))
            return false;

        if (!ValidatePairedPowerTime(rows, 4, 11, out error))
            return false;

        if (!ValidatePairedPowerTime(rows, 5, 12, out error))
            return false;

        if (!ValidateStationPair(rows[5], rows[12], "Nсетев и Тсетев", out error))
            return false;

        if (!ValidateEmergencyTriplet(rows[6], rows[13], rows[14], out error))
            return false;

        var release = rows[15].Station;
        if (release <= 0)
        {
            error = "Укажите отпуск электроэнергии (Эj, млн кВт·ч).";
            return false;
        }

        return true;
    }

    private static bool ValidateRowValues(PerejegInputRow row, out string? error)
    {
        error = null;

        for (var block = 1; block <= 12; block++)
        {
            var value = row.GetBlock(block);
            if (value < 0 || value > MaxValue)
            {
                error = $"Строка «{row.Label}», блок {block}: допустимый диапазон 0…{MaxValue:0.###}.";
                return false;
            }
        }

        if (row.Station < 0 || row.Station > MaxValue)
        {
            error = $"Строка «{row.Label}», общие показатели: допустимый диапазон 0…{MaxValue:0.###}.";
            return false;
        }

        return true;
    }

    private static bool ValidatePairedPowerTime(
        IReadOnlyList<PerejegInputRow> rows,
        int powerIndex,
        int timeIndex,
        out string? error)
    {
        error = null;
        var power = rows[powerIndex - 1];
        var time = rows[timeIndex - 1];

        for (var block = 1; block <= 12; block++)
        {
            var n = power.GetBlock(block);
            var t = time.GetBlock(block);
            if (IsZero(n) != IsZero(t))
            {
                error =
                    $"Строки «{power.Label}» и «{time.Label}», блок {block}: мощность и время должны быть указаны вместе.";
                return false;
            }
        }

        return true;
    }

    private static bool ValidateStationPair(
        PerejegInputRow power,
        PerejegInputRow time,
        string label,
        out string? error)
    {
        error = null;
        if (IsZero(power.Station) != IsZero(time.Station))
        {
            error = $"{label}: мощность и время должны быть указаны вместе.";
            return false;
        }

        return true;
    }

    private static bool ValidateEmergencyTriplet(
        PerejegInputRow power,
        PerejegInputRow time,
        PerejegInputRow deltaT,
        out string? error)
    {
        error = null;

        for (var block = 1; block <= 12; block++)
        {
            var n = power.GetBlock(block);
            var t = time.GetBlock(block);
            var dt = deltaT.GetBlock(block);
            var allZero = IsZero(n) && IsZero(t) && IsZero(dt);
            var allFilled = !IsZero(n) && !IsZero(t) && !IsZero(dt);

            if (!allZero && !allFilled)
            {
                error =
                    $"Авар. впрыск, блок {block}: укажите N, T и DT вместе или оставьте все три поля пустыми.";
                return false;
            }
        }

        return true;
    }

    private static bool IsZero(decimal value) => value == 0m;
}
