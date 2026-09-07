using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Opto.Models;
using Opto.Services.Database;

namespace Opto.Services;

public static class WyrabotkaStore
{
    public static WyrabotkaReport? TryLoadResult(DateTime date)
    {
        using var connection = OptoDatabase.OpenConnection();
        var snapshot = TryLoad(connection, date);
        if (snapshot is null)
            return null;

        var mode = TryLoadMode(date) ?? WyrabotkaMode.Calculation;
        var monthBefore = GetMonthTotalsBefore(connection, date);
        return WyrabotkaCalculator.Calculate(date, mode, snapshot.ToBlockRows(), snapshot.ToTransformerRows(), monthBefore);
    }
    public static WyrabotkaMode? TryLoadMode(DateTime date)
    {
        using var connection = OptoDatabase.OpenConnection();
        var dateKey = OptoDatabase.DateKey(date);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT mode FROM wyrabotka_day WHERE date = @date LIMIT 1;";
        OptoDatabase.AddParameter(command, "@date", dateKey);
        var result = command.ExecuteScalar();
        return result is long mode ? (WyrabotkaMode)mode : null;
    }

    public static DateTime? GetLatestDay()
    {
        using var connection = OptoDatabase.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT date FROM wyrabotka_day ORDER BY date DESC LIMIT 1;";
        var value = command.ExecuteScalar();
        return value is string text ? DateTime.Parse(text) : null;
    }

    public static void Save(DateTime date, WyrabotkaMode mode, WyrabotkaEditSnapshot snapshot)
    {
        using var connection = OptoDatabase.OpenConnection();
        Save(connection, date, mode, snapshot);
    }

    public static WyrabotkaEditSnapshot? TryLoad(DateTime date)
    {
        using var connection = OptoDatabase.OpenConnection();
        return TryLoad(connection, date);
    }

    public static void SaveResult(DateTime date, WyrabotkaReport report)
    {
        using var connection = OptoDatabase.OpenConnection();
        SaveResult(connection, date, report);
    }

    public static WyrabotkaMonthTotals GetMonthTotalsBefore(DateTime date)
    {
        using var connection = OptoDatabase.OpenConnection();
        return GetMonthTotalsBefore(connection, date);
    }

    internal static bool Exists(SqliteConnection connection, string dateKey)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM wyrabotka_day WHERE date = @date LIMIT 1;";
        OptoDatabase.AddParameter(command, "@date", dateKey);
        return command.ExecuteScalar() is not null;
    }

    internal static void Save(
        SqliteConnection connection,
        DateTime date,
        WyrabotkaMode mode,
        WyrabotkaEditSnapshot snapshot)
    {
        var dateKey = OptoDatabase.DateKey(date);
        using var transaction = connection.BeginTransaction();

        OptoDatabase.ExecuteNonQuery(connection, "DELETE FROM wyrabotka_day WHERE date = @date;", ("@date", dateKey));
        OptoDatabase.ExecuteNonQuery(connection, """
            INSERT INTO wyrabotka_day (date, mode, saved_at)
            VALUES (@date, @mode, @savedAt);
            """,
            ("@date", dateKey),
            ("@mode", (int)mode),
            ("@savedAt", OptoDatabase.Timestamp(DateTime.Now)));

        foreach (var block in snapshot.Blocks)
        {
            OptoDatabase.ExecuteNonQuery(connection, """
                INSERT INTO wyrabotka_block (
                    date, number, generation_coefficient, generation_start, generation_end,
                    own_needs_coefficient, own_needs_start, own_needs_end, hours)
                VALUES (
                    @date, @number, @genCoeff, @genStart, @genEnd,
                    @snCoeff, @snStart, @snEnd, @hours);
                """,
                ("@date", dateKey),
                ("@number", block.Number),
                ("@genCoeff", block.GenerationCoefficient),
                ("@genStart", block.GenerationStart),
                ("@genEnd", block.GenerationEnd),
                ("@snCoeff", block.OwnNeedsCoefficient),
                ("@snStart", block.OwnNeedsStart),
                ("@snEnd", block.OwnNeedsEnd),
                ("@hours", block.Hours));
        }

        foreach (var transformer in snapshot.Transformers)
        {
            OptoDatabase.ExecuteNonQuery(connection, """
                INSERT INTO wyrabotka_transformer (date, name, coefficient, start_value, end_value)
                VALUES (@date, @name, @coefficient, @start, @end);
                """,
                ("@date", dateKey),
                ("@name", transformer.Name),
                ("@coefficient", transformer.Coefficient),
                ("@start", transformer.Start),
                ("@end", transformer.End));
        }

        transaction.Commit();
    }

    internal static WyrabotkaEditSnapshot? TryLoad(SqliteConnection connection, DateTime date)
    {
        var dateKey = OptoDatabase.DateKey(date);
        using var dayCommand = connection.CreateCommand();
        dayCommand.CommandText = "SELECT 1 FROM wyrabotka_day WHERE date = @date LIMIT 1;";
        OptoDatabase.AddParameter(dayCommand, "@date", dateKey);
        if (dayCommand.ExecuteScalar() is null)
            return null;

        var snapshot = new WyrabotkaEditSnapshot();

        using (var blockCommand = connection.CreateCommand())
        {
            blockCommand.CommandText = """
                SELECT number, generation_coefficient, generation_start, generation_end,
                       own_needs_coefficient, own_needs_start, own_needs_end, hours
                FROM wyrabotka_block
                WHERE date = @date
                ORDER BY number;
                """;
            OptoDatabase.AddParameter(blockCommand, "@date", dateKey);
            using var reader = blockCommand.ExecuteReader();
            var blocks = new System.Collections.Generic.List<WyrabotkaBlockData>();
            while (reader.Read())
            {
                blocks.Add(new WyrabotkaBlockData
                {
                    Number = reader.GetInt32(0),
                    GenerationCoefficient = reader.GetDecimal(1),
                    GenerationStart = reader.GetDecimal(2),
                    GenerationEnd = reader.GetDecimal(3),
                    OwnNeedsCoefficient = reader.GetDecimal(4),
                    OwnNeedsStart = reader.GetDecimal(5),
                    OwnNeedsEnd = reader.GetDecimal(6),
                    Hours = reader.GetInt32(7),
                });
            }

            snapshot.Blocks = blocks.ToArray();
        }

        using (var transformerCommand = connection.CreateCommand())
        {
            transformerCommand.CommandText = """
                SELECT name, coefficient, start_value, end_value
                FROM wyrabotka_transformer
                WHERE date = @date
                ORDER BY name;
                """;
            OptoDatabase.AddParameter(transformerCommand, "@date", dateKey);
            using var reader = transformerCommand.ExecuteReader();
            var transformers = new System.Collections.Generic.List<WyrabotkaTransformerData>();
            while (reader.Read())
            {
                transformers.Add(new WyrabotkaTransformerData
                {
                    Name = reader.GetString(0),
                    Coefficient = reader.GetDecimal(1),
                    Start = reader.GetDecimal(2),
                    End = reader.GetDecimal(3),
                });
            }

            snapshot.Transformers = transformers.ToArray();
        }

        return snapshot;
    }

    internal static void SaveResult(SqliteConnection connection, DateTime date, WyrabotkaReport report)
    {
        var dateKey = OptoDatabase.DateKey(date);
        OptoDatabase.ExecuteNonQuery(connection, "DELETE FROM wyrabotka_result WHERE date = @date;", ("@date", dateKey));
        OptoDatabase.ExecuteNonQuery(connection, """
            INSERT INTO wyrabotka_result (
                date, day_hours, day_generation, day_own_needs, day_release, calculated_at)
            VALUES (@date, @hours, @generation, @ownNeeds, @release, @calculatedAt);
            """,
            ("@date", dateKey),
            ("@hours", report.DayTotal.Hours ?? 0),
            ("@generation", report.DayTotal.GenerationThousandKwh),
            ("@ownNeeds", report.DayTotal.OwnNeedsThousandKwh),
            ("@release", report.DayRelease),
            ("@calculatedAt", OptoDatabase.Timestamp(DateTime.Now)));
    }

    internal static WyrabotkaMonthTotals GetMonthTotalsBefore(SqliteConnection connection, DateTime date)
    {
        var monthStart = OptoDatabase.DateKey(new DateTime(date.Year, date.Month, 1));
        var currentDate = OptoDatabase.DateKey(date);

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                COALESCE(SUM(day_hours), 0),
                COALESCE(SUM(day_generation), 0),
                COALESCE(SUM(day_own_needs), 0),
                COALESCE(SUM(day_release), 0)
            FROM wyrabotka_result
            WHERE date >= @monthStart AND date < @currentDate;
            """;
        OptoDatabase.AddParameter(command, "@monthStart", monthStart);
        OptoDatabase.AddParameter(command, "@currentDate", currentDate);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return new WyrabotkaMonthTotals();
        }

        return new WyrabotkaMonthTotals
        {
            Hours = reader.GetInt32(0),
            GenerationThousandKwh = reader.GetDecimal(1),
            OwnNeedsThousandKwh = reader.GetDecimal(2),
            ReleaseThousandKwh = reader.GetDecimal(3),
        };
    }

    public static DateTime? GetLatestDayBefore(DateTime date)
    {
        using var connection = OptoDatabase.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT date FROM wyrabotka_day WHERE date < @date ORDER BY date DESC LIMIT 1;";
        OptoDatabase.AddParameter(command, "@date", OptoDatabase.DateKey(date));
        var value = command.ExecuteScalar();
        return value is string text ? DateTime.Parse(text) : null;
    }

    public static WyrabotkaEditSnapshot? TryLoadTemplateSnapshot(DateTime date)
    {
        using var connection = OptoDatabase.OpenConnection();
        if (TryLoad(connection, date) is not null)
            return null;

        var prevDate = GetLatestDayBefore(date);
        if (prevDate is null)
            return null;

        var source = TryLoad(connection, prevDate.Value);
        if (source is null)
            return null;

        var clone = new WyrabotkaEditSnapshot
        {
            Blocks = source.Blocks.Select(b => new WyrabotkaBlockData
            {
                Number = b.Number,
                GenerationCoefficient = b.GenerationCoefficient,
                GenerationStart = b.GenerationEnd,
                GenerationEnd = 0,
                OwnNeedsCoefficient = b.OwnNeedsCoefficient,
                OwnNeedsStart = b.OwnNeedsEnd,
                OwnNeedsEnd = 0,
                Hours = b.Hours,
            }).ToArray(),
            Transformers = source.Transformers.Select(t => new WyrabotkaTransformerData
            {
                Name = t.Name,
                Coefficient = t.Coefficient,
                Start = t.End,
                End = 0,
            }).ToArray(),
        };

        return clone;
    }
}
