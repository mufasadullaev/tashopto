namespace Opto.Models;

public enum BaxtaMenuAction
{
    ViewWatchSchedule,
    EditData,
    PrintForms,
    ViewDates,
    CopyData,
}

/// <summary>Строка графика вахт (karat BAXTA0, 8 строк).</summary>
public sealed class BaxtaWatchScheduleRow
{
    public required int RowIndex { get; init; }
    public required int Shift1Watch { get; init; }
    public required int Shift2Watch { get; init; }
    public required int Shift3Watch { get; init; }
    public required int RestingWatch { get; init; }
    public required bool IsNext { get; init; }
    /// <summary>Дата последнего расчёта по этой строке, формат ММДД (0 — не использовалась).</summary>
    public required int LastDateMmdd { get; init; }

    public string Shift1WatchLetter => BaxtaWatchLetters.FromNumber(Shift1Watch);
    public string Shift2WatchLetter => BaxtaWatchLetters.FromNumber(Shift2Watch);
    public string Shift3WatchLetter => BaxtaWatchLetters.FromNumber(Shift3Watch);
    public string RestingWatchLetter => BaxtaWatchLetters.FromNumber(RestingWatch);

    public string LastDateText => LastDateMmdd == 0
        ? "—"
        : $"{LastDateMmdd % 100:00}.{(LastDateMmdd / 100):00}";
}

public static class BaxtaWatchLetters
{
    public static readonly string[] All = ["А", "Б", "В", "Г"];

    public static string FromNumber(int watch) =>
        watch is >= 1 and <= 4 ? All[watch - 1] : "?";
}
