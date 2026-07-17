using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Opto.Models;
using Opto.Services.Database;

namespace Opto.Services;

public static class PerejegStore
{
    public static void Save(DateTime date, WyrabotkaMode mode, PerejegEditSnapshot snapshot)
    {
        using var connection = OptoDatabase.OpenConnection();
        Save(connection, date, mode, snapshot);
    }

    public static PerejegEditSnapshot? TryLoad(DateTime date)
    {
        using var connection = OptoDatabase.OpenConnection();
        return TryLoad(connection, date);
    }

    public static WyrabotkaMode? TryLoadMode(DateTime date)
    {
        using var connection = OptoDatabase.OpenConnection();
        return TryLoadMode(connection, date);
    }

    public static bool HasResult(DateTime date)
    {
        using var connection = OptoDatabase.OpenConnection();
        var dateKey = OptoDatabase.DateKey(date);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM perejeg_result WHERE date = @date LIMIT 1;";
        OptoDatabase.AddParameter(command, "@date", dateKey);
        return command.ExecuteScalar() is not null;
    }

    public static PerejegReport? TryLoadReport(DateTime date)
    {
        if (!HasResult(date))
            return null;

        var snapshot = TryLoad(date);
        if (snapshot is null)
            return null;

        var mode = TryLoadMode(date) ?? WyrabotkaMode.Calculation;
        var rows = PerejegInputFactory.CreateEmptyRows();
        snapshot.ApplyTo(rows);
        return PerejegCalculator.Calculate(date, mode, rows);
    }

    public static IReadOnlyList<DateTime> ListCalculatedDates()
    {
        using var connection = OptoDatabase.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT date FROM perejeg_result ORDER BY date;";

        var dates = new List<DateTime>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            dates.Add(DateTime.Parse(reader.GetString(0)));

        return dates;
    }

    public static IReadOnlyList<PerejegResultData> LoadResultsInRange(DateTime from, DateTime to)
    {
        using var connection = OptoDatabase.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT result_json
            FROM perejeg_result
            WHERE date >= @from AND date <= @to
            ORDER BY date;
            """;
        OptoDatabase.AddParameter(command, "@from", OptoDatabase.DateKey(from));
        OptoDatabase.AddParameter(command, "@to", OptoDatabase.DateKey(to));

        var results = new List<PerejegResultData>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var data = PerejegResultData.FromJson(reader.GetString(0));
            if (data is not null)
                results.Add(data);
        }

        return results;
    }

    public static PerejegReport? TryBuildCumulativeReport(DateTime from, DateTime to)
    {
        var results = LoadResultsInRange(from, to);
        if (results.Count == 0)
            return null;

        return PerejegCalculator.BuildCumulativeReport(from, to, results);
    }

    public static void SaveResult(DateTime date, PerejegReport report)
    {
        using var connection = OptoDatabase.OpenConnection();
        SaveResult(connection, date, report);
    }

    public static decimal? TryGetReleaseMlnKwh(DateTime date)
    {
        using var connection = OptoDatabase.OpenConnection();
        var dateKey = OptoDatabase.DateKey(date);

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT day_release
            FROM wyrabotka_result
            WHERE date = @date
            LIMIT 1;
            """;
        OptoDatabase.AddParameter(command, "@date", dateKey);

        var value = command.ExecuteScalar();
        if (value is null or DBNull)
            return null;

        var thousandKwh = Convert.ToDecimal(value);
        return thousandKwh / 1000m;
    }

    internal static void Save(
        SqliteConnection connection,
        DateTime date,
        WyrabotkaMode mode,
        PerejegEditSnapshot snapshot)
    {
        var dateKey = OptoDatabase.DateKey(date);
        using var transaction = connection.BeginTransaction();

        OptoDatabase.ExecuteNonQuery(connection, "DELETE FROM perejeg_day WHERE date = @date;", ("@date", dateKey));
        OptoDatabase.ExecuteNonQuery(connection, """
            INSERT INTO perejeg_day (date, mode, saved_at)
            VALUES (@date, @mode, @savedAt);
            """,
            ("@date", dateKey),
            ("@mode", (int)mode),
            ("@savedAt", OptoDatabase.Timestamp(DateTime.Now)));

        foreach (var row in snapshot.Rows)
        {
            OptoDatabase.ExecuteNonQuery(connection, """
                INSERT INTO perejeg_row (
                    date, row_index,
                    block1, block2, block3, block4, block5, block6,
                    block7, block8, block9, block10, block11, block12,
                    station)
                VALUES (
                    @date, @rowIndex,
                    @b1, @b2, @b3, @b4, @b5, @b6,
                    @b7, @b8, @b9, @b10, @b11, @b12,
                    @station);
                """,
                ("@date", dateKey),
                ("@rowIndex", row.Index),
                ("@b1", row.Block1),
                ("@b2", row.Block2),
                ("@b3", row.Block3),
                ("@b4", row.Block4),
                ("@b5", row.Block5),
                ("@b6", row.Block6),
                ("@b7", row.Block7),
                ("@b8", row.Block8),
                ("@b9", row.Block9),
                ("@b10", row.Block10),
                ("@b11", row.Block11),
                ("@b12", row.Block12),
                ("@station", row.Station));
        }

        transaction.Commit();
    }

    internal static PerejegEditSnapshot? TryLoad(SqliteConnection connection, DateTime date)
    {
        var dateKey = OptoDatabase.DateKey(date);
        using var dayCommand = connection.CreateCommand();
        dayCommand.CommandText = "SELECT 1 FROM perejeg_day WHERE date = @date LIMIT 1;";
        OptoDatabase.AddParameter(dayCommand, "@date", dateKey);
        if (dayCommand.ExecuteScalar() is null)
            return null;

        var snapshot = new PerejegEditSnapshot();
        var rows = new List<PerejegRowData>();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT row_index,
                   block1, block2, block3, block4, block5, block6,
                   block7, block8, block9, block10, block11, block12,
                   station
            FROM perejeg_row
            WHERE date = @date
            ORDER BY row_index;
            """;
        OptoDatabase.AddParameter(command, "@date", dateKey);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(new PerejegRowData
            {
                Index = reader.GetInt32(0),
                Block1 = reader.GetDecimal(1),
                Block2 = reader.GetDecimal(2),
                Block3 = reader.GetDecimal(3),
                Block4 = reader.GetDecimal(4),
                Block5 = reader.GetDecimal(5),
                Block6 = reader.GetDecimal(6),
                Block7 = reader.GetDecimal(7),
                Block8 = reader.GetDecimal(8),
                Block9 = reader.GetDecimal(9),
                Block10 = reader.GetDecimal(10),
                Block11 = reader.GetDecimal(11),
                Block12 = reader.GetDecimal(12),
                Station = reader.GetDecimal(13),
            });
        }

        snapshot.Rows = rows.ToArray();
        return snapshot;
    }

    internal static WyrabotkaMode? TryLoadMode(SqliteConnection connection, DateTime date)
    {
        var dateKey = OptoDatabase.DateKey(date);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT mode FROM perejeg_day WHERE date = @date LIMIT 1;";
        OptoDatabase.AddParameter(command, "@date", dateKey);
        var value = command.ExecuteScalar();
        return value is null or DBNull ? null : (WyrabotkaMode)Convert.ToInt32(value);
    }

    internal static void SaveResult(SqliteConnection connection, DateTime date, PerejegReport report)
    {
        var dateKey = OptoDatabase.DateKey(date);
        var payload = PerejegCalculator.BuildResultData(report).ToJson();

        OptoDatabase.ExecuteNonQuery(connection, "DELETE FROM perejeg_result WHERE date = @date;", ("@date", dateKey));
        OptoDatabase.ExecuteNonQuery(connection, """
            INSERT INTO perejeg_result (date, calculated_at, release_mln_kwh, total_gkwh, result_json)
            VALUES (@date, @calculatedAt, @release, @totalGkwh, @json);
            """,
            ("@date", dateKey),
            ("@calculatedAt", OptoDatabase.Timestamp(DateTime.Now)),
            ("@release", report.ReleaseMlnKwh),
            ("@totalGkwh", report.TotalGkwh),
            ("@json", payload));
    }
}
