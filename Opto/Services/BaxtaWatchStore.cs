using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Opto.Models;
using Opto.Services.Database;

namespace Opto.Services;

public static class BaxtaWatchStore
{
    /// <summary>Шаблон из karat BAXTA0.DBF — 8 строк, цикл ~8 суток.</summary>
    private static readonly (int S1, int S2, int S3, int RestWatch, int Mmdd, bool Next)[] KaratDefaults =
    [
        (3, 1, 4, 2, 415, false),
        (1, 2, 4, 3, 416, false),
        (1, 2, 3, 4, 417, false),
        (2, 4, 3, 1, 418, false),
        (2, 4, 1, 3, 419, false),
        (4, 3, 1, 2, 420, false),
        (4, 3, 2, 1, 413, true),
        (3, 1, 2, 4, 414, false),
    ];

    public static IReadOnlyList<BaxtaWatchScheduleRow> LoadSchedule()
    {
        using var connection = OptoDatabase.OpenConnection();
        EnsureInitialized(connection);
        return LoadSchedule(connection);
    }

    public static int[] ResolveWatches(DateTime date, WyrabotkaMode mode)
    {
        using var connection = OptoDatabase.OpenConnection();
        EnsureInitialized(connection);
        var rows = LoadSchedule(connection);
        var row = FindRowForDate(rows, date, mode);

        if (row is null)
            return [1, 2, 3]; // Default fallback watch rotation (Brigate A, B, V)

        return [row.Shift1Watch, row.Shift2Watch, row.Shift3Watch];
    }

    public static string? TryValidate(DateTime date, WyrabotkaMode mode)
    {
        using var connection = OptoDatabase.OpenConnection();
        EnsureInitialized(connection);
        var rows = LoadSchedule(connection);
        var mmdd = ToMmdd(date);

        var hasSavedData = BaxtaStore.HasDay(date) || rows.Any(r => r.LastDateMmdd == mmdd);

        if (mode == WyrabotkaMode.Recalculation)
        {
            if (!hasSavedData)
                return "За эту дату ещё нет сохранённых данных. Выберите режим «Расчёт».";
            return null;
        }

        if (hasSavedData)
            return "За эту дату уже был выполнен первичный расчёт. Выберите режим «Перерасчёт».";

        return null;
    }

    /// <summary>Сброс графика вахт при переходе на новый месяц (karat «Менять месяц?»).</summary>
    public static void ResetForNewMonth()
    {
        using var connection = OptoDatabase.OpenConnection();
        EnsureInitialized(connection);
        OptoDatabase.ExecuteNonQuery(connection, "DELETE FROM baxta_watch_schedule;");

        for (var i = 0; i < KaratDefaults.Length; i++)
        {
            var d = KaratDefaults[i];
            OptoDatabase.ExecuteNonQuery(connection, """
                INSERT INTO baxta_watch_schedule (
                    row_index, shift1_watch, shift2_watch, shift3_watch,
                    resting_watch, is_next, last_date_mmdd)
                VALUES (
                    @row, @s1, @s2, @s3, @rest, @next, @mmdd);
                """,
                ("@row", i + 1),
                ("@s1", d.S1),
                ("@s2", d.S2),
                ("@s3", d.S3),
                ("@rest", d.RestWatch),
                ("@next", d.Next ? 1 : 0),
                ("@mmdd", d.Mmdd));
        }
    }

    public static void AdvanceAfterCalculation(DateTime date)
    {
        using var connection = OptoDatabase.OpenConnection();
        EnsureInitialized(connection);
        var rows = LoadSchedule(connection).ToList();
        var nextRow = rows.FirstOrDefault(r => r.IsNext)
            ?? throw new InvalidOperationException("График вахт: не найдена следующая строка.");

        var mmdd = ToMmdd(date);
        OptoDatabase.ExecuteNonQuery(connection,
            """
            UPDATE baxta_watch_schedule
            SET is_next = 0, last_date_mmdd = @mmdd
            WHERE row_index = @row;
            """,
            ("@mmdd", mmdd),
            ("@row", nextRow.RowIndex));

        var newNext = nextRow.RowIndex == 8 ? 1 : nextRow.RowIndex + 1;
        OptoDatabase.ExecuteNonQuery(connection,
            """
            UPDATE baxta_watch_schedule
            SET is_next = 1
            WHERE row_index = @row;
            """,
            ("@row", newNext));
    }

    internal static int ToMmdd(DateTime date) => date.Day + date.Month * 100;

    private static BaxtaWatchScheduleRow? FindRowForDate(
        IReadOnlyList<BaxtaWatchScheduleRow> rows,
        DateTime date,
        WyrabotkaMode mode)
    {
        var mmdd = ToMmdd(date);

        if (mode == WyrabotkaMode.Recalculation)
        {
            var match = rows.FirstOrDefault(r => r.LastDateMmdd == mmdd);
            if (match is not null) return match;
        }

        return rows.FirstOrDefault(r => r.IsNext) ?? rows.FirstOrDefault();
    }

    private static void EnsureInitialized(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM baxta_watch_schedule;";
        var count = Convert.ToInt32(command.ExecuteScalar());
        if (count > 0)
            return;

        for (var i = 0; i < KaratDefaults.Length; i++)
        {
            var d = KaratDefaults[i];
            OptoDatabase.ExecuteNonQuery(connection, """
                INSERT INTO baxta_watch_schedule (
                    row_index, shift1_watch, shift2_watch, shift3_watch,
                    resting_watch, is_next, last_date_mmdd)
                VALUES (
                    @row, @s1, @s2, @s3, @rest, @next, @mmdd);
                """,
                ("@row", i + 1),
                ("@s1", d.S1),
                ("@s2", d.S2),
                ("@s3", d.S3),
                ("@rest", d.RestWatch),
                ("@next", d.Next ? 1 : 0),
                ("@mmdd", d.Mmdd));
        }
    }

    private static List<BaxtaWatchScheduleRow> LoadSchedule(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT row_index, shift1_watch, shift2_watch, shift3_watch,
                   resting_watch, is_next, last_date_mmdd
            FROM baxta_watch_schedule
            ORDER BY row_index;
            """;

        var rows = new List<BaxtaWatchScheduleRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(new BaxtaWatchScheduleRow
            {
                RowIndex = reader.GetInt32(0),
                Shift1Watch = reader.GetInt32(1),
                Shift2Watch = reader.GetInt32(2),
                Shift3Watch = reader.GetInt32(3),
                RestingWatch = reader.GetInt32(4),
                IsNext = reader.GetInt32(5) == 1,
                LastDateMmdd = reader.GetInt32(6),
            });
        }

        return rows;
    }
}
