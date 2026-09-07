using System;

namespace Opto.Models;

/// <summary>
/// Одна запись смены блока — аналог строки BAXTA50 в karat (до деления на 1000).
/// </summary>
public sealed class BaxtaShiftDetail
{
    public required DateTime Date { get; init; }
    public required int BlockNumber { get; init; }
    public required int ShiftIndex { get; init; }
    /// <summary>Номер вахты 1–4 (А–Г) по графику BAXTA0.</summary>
    public required int WatchIndex { get; init; }
    public required int OperatorTn { get; init; }
    /// <summary>Нагрузка, МВт (snnb в karat).</summary>
    public required double Load { get; init; }
    public required double Pug { get; init; }
    public required double Wak { get; init; }
    public required double Dop { get; init; }
    public required double Top { get; init; }
    public required double Tpp { get; init; }
    public required double Sn { get; init; }
    public required double Tpw { get; init; }
}
