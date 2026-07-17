using System;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Opto.Models;
using Opto.Services.Database;

namespace Opto.Services;

public static class BaxtaStore
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public static void Save(DateTime date, WyrabotkaMode mode, BaxtaEditSnapshot snapshot)
    {
        using var connection = OptoDatabase.OpenConnection();
        Save(connection, date, mode, snapshot);
    }

    public static BaxtaEditSnapshot? TryLoad(DateTime date)
    {
        using var connection = OptoDatabase.OpenConnection();
        return TryLoad(connection, date);
    }

    public static void SaveResult(DateTime date, BaxtaReport report)
    {
        using var connection = OptoDatabase.OpenConnection();
        SaveResult(connection, date, report);
    }

    internal static bool Exists(SqliteConnection connection, string dateKey)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM baxta_day WHERE date = @date LIMIT 1;";
        OptoDatabase.AddParameter(command, "@date", dateKey);
        return command.ExecuteScalar() is not null;
    }

    internal static void Save(
        SqliteConnection connection,
        DateTime date,
        WyrabotkaMode mode,
        BaxtaEditSnapshot snapshot)
    {
        var dateKey = OptoDatabase.DateKey(date);
        using var transaction = connection.BeginTransaction();

        OptoDatabase.ExecuteNonQuery(connection, "DELETE FROM baxta_day WHERE date = @date;", ("@date", dateKey));
        OptoDatabase.ExecuteNonQuery(connection, """
            INSERT INTO baxta_day (date, mode, saved_at)
            VALUES (@date, @mode, @savedAt);
            """,
            ("@date", dateKey),
            ("@mode", (int)mode),
            ("@savedAt", OptoDatabase.Timestamp(DateTime.Now)));

        foreach (var meter in snapshot.Meters)
        {
            OptoDatabase.ExecuteNonQuery(connection, """
                INSERT INTO baxta_meter (
                    date, number, generation_coefficient, generation_at0, generation_at8,
                    generation_at16, generation_at24, own_needs_coefficient, own_needs_at0,
                    own_needs_at8, own_needs_at16, own_needs_at24)
                VALUES (
                    @date, @number, @genCoeff, @gen0, @gen8, @gen16, @gen24,
                    @snCoeff, @sn0, @sn8, @sn16, @sn24);
                """,
                ("@date", dateKey),
                ("@number", meter.Number),
                ("@genCoeff", meter.GenerationCoefficient),
                ("@gen0", meter.GenerationAt0),
                ("@gen8", meter.GenerationAt8),
                ("@gen16", meter.GenerationAt16),
                ("@gen24", meter.GenerationAt24),
                ("@snCoeff", meter.OwnNeedsCoefficient),
                ("@sn0", meter.OwnNeedsAt0),
                ("@sn8", meter.OwnNeedsAt8),
                ("@sn16", meter.OwnNeedsAt16),
                ("@sn24", meter.OwnNeedsAt24));
        }

        foreach (var coeff in snapshot.Coeffs)
        {
            OptoDatabase.ExecuteNonQuery(connection, """
                INSERT INTO baxta_coeff (date, number, kl1, kl2, kf1, kf2, kf3, kf4)
                VALUES (@date, @number, @kl1, @kl2, @kf1, @kf2, @kf3, @kf4);
                """,
                ("@date", dateKey),
                ("@number", coeff.Number),
                ("@kl1", coeff.Kl1),
                ("@kl2", coeff.Kl2),
                ("@kf1", coeff.Kf1),
                ("@kf2", coeff.Kf2),
                ("@kf3", coeff.Kf3),
                ("@kf4", coeff.Kf4));
        }

        foreach (var thermo in snapshot.Thermo)
        {
            OptoDatabase.ExecuteNonQuery(connection, """
                INSERT INTO baxta_thermo (
                    date, block_number, shift_index, shift_label, hours, dro, pwd, tpw,
                    tk, top, tpp, pop, tug, thw, o2, tn)
                VALUES (
                    @date, @block, @shift, @label, @hours, @dro, @pwd, @tpw,
                    @tk, @top, @tpp, @pop, @tug, @thw, @o2, @tn);
                """,
                ("@date", dateKey),
                ("@block", thermo.BlockNumber),
                ("@shift", thermo.ShiftIndex),
                ("@label", thermo.ShiftLabel),
                ("@hours", thermo.Hours),
                ("@dro", thermo.Dro),
                ("@pwd", thermo.Pwd),
                ("@tpw", thermo.Tpw),
                ("@tk", thermo.Tk),
                ("@top", thermo.Top),
                ("@tpp", thermo.Tpp),
                ("@pop", thermo.Pop),
                ("@tug", thermo.Tug),
                ("@thw", thermo.Thw),
                ("@o2", thermo.O2),
                ("@tn", thermo.Tn));
        }

        var plant = snapshot.Plant;
        OptoDatabase.ExecuteNonQuery(connection, """
            INSERT INTO baxta_plant (
                date, urp, urm, tcb1, tcb2, tcb3,
                wrmn11, wrmn12, wrmn13, wrmn21, wrmn22, wrmn23,
                wrmn31, wrmn32, wrmn33, wrmn14, wrmn24, wrmn34,
                setn1k, setn1n, setn2k, setn2n, setn3k, setn3n,
                setn4k, setn4n, setn5k, setn5n, prises_json)
            VALUES (
                @date, @urp, @urm, @tcb1, @tcb2, @tcb3,
                @wrmn11, @wrmn12, @wrmn13, @wrmn21, @wrmn22, @wrmn23,
                @wrmn31, @wrmn32, @wrmn33, @wrmn14, @wrmn24, @wrmn34,
                @setn1k, @setn1n, @setn2k, @setn2n, @setn3k, @setn3n,
                @setn4k, @setn4n, @setn5k, @setn5n, @prisesJson);
            """,
            ("@date", dateKey),
            ("@urp", plant.Urp),
            ("@urm", plant.Urm),
            ("@tcb1", plant.Tcb1),
            ("@tcb2", plant.Tcb2),
            ("@tcb3", plant.Tcb3),
            ("@wrmn11", plant.Wrmn11),
            ("@wrmn12", plant.Wrmn12),
            ("@wrmn13", plant.Wrmn13),
            ("@wrmn21", plant.Wrmn21),
            ("@wrmn22", plant.Wrmn22),
            ("@wrmn23", plant.Wrmn23),
            ("@wrmn31", plant.Wrmn31),
            ("@wrmn32", plant.Wrmn32),
            ("@wrmn33", plant.Wrmn33),
            ("@wrmn14", plant.Wrmn14),
            ("@wrmn24", plant.Wrmn24),
            ("@wrmn34", plant.Wrmn34),
            ("@setn1k", plant.Setn1K),
            ("@setn1n", plant.Setn1N),
            ("@setn2k", plant.Setn2K),
            ("@setn2n", plant.Setn2N),
            ("@setn3k", plant.Setn3K),
            ("@setn3n", plant.Setn3N),
            ("@setn4k", plant.Setn4K),
            ("@setn4n", plant.Setn4N),
            ("@setn5k", plant.Setn5K),
            ("@setn5n", plant.Setn5N),
            ("@prisesJson", JsonSerializer.Serialize(plant.Prises, JsonOptions)));

        transaction.Commit();
    }

    internal static BaxtaEditSnapshot? TryLoad(SqliteConnection connection, DateTime date)
    {
        var dateKey = OptoDatabase.DateKey(date);
        using var dayCommand = connection.CreateCommand();
        dayCommand.CommandText = "SELECT 1 FROM baxta_day WHERE date = @date LIMIT 1;";
        OptoDatabase.AddParameter(dayCommand, "@date", dateKey);
        if (dayCommand.ExecuteScalar() is null)
            return null;

        var snapshot = new BaxtaEditSnapshot();

        using (var meterCommand = connection.CreateCommand())
        {
            meterCommand.CommandText = """
                SELECT number, generation_coefficient, generation_at0, generation_at8,
                       generation_at16, generation_at24, own_needs_coefficient,
                       own_needs_at0, own_needs_at8, own_needs_at16, own_needs_at24
                FROM baxta_meter
                WHERE date = @date
                ORDER BY number;
                """;
            OptoDatabase.AddParameter(meterCommand, "@date", dateKey);
            using var reader = meterCommand.ExecuteReader();
            var meters = new System.Collections.Generic.List<BaxtaMeterData>();
            while (reader.Read())
            {
                meters.Add(new BaxtaMeterData
                {
                    Number = reader.GetInt32(0),
                    GenerationCoefficient = reader.GetDecimal(1),
                    GenerationAt0 = reader.GetDecimal(2),
                    GenerationAt8 = reader.GetDecimal(3),
                    GenerationAt16 = reader.GetDecimal(4),
                    GenerationAt24 = reader.GetDecimal(5),
                    OwnNeedsCoefficient = reader.GetDecimal(6),
                    OwnNeedsAt0 = reader.GetDecimal(7),
                    OwnNeedsAt8 = reader.GetDecimal(8),
                    OwnNeedsAt16 = reader.GetDecimal(9),
                    OwnNeedsAt24 = reader.GetDecimal(10),
                });
            }

            snapshot.Meters = meters.ToArray();
        }

        using (var coeffCommand = connection.CreateCommand())
        {
            coeffCommand.CommandText = """
                SELECT number, kl1, kl2, kf1, kf2, kf3, kf4
                FROM baxta_coeff
                WHERE date = @date
                ORDER BY number;
                """;
            OptoDatabase.AddParameter(coeffCommand, "@date", dateKey);
            using var reader = coeffCommand.ExecuteReader();
            var coeffs = new System.Collections.Generic.List<BaxtaCoeffsData>();
            while (reader.Read())
            {
                coeffs.Add(new BaxtaCoeffsData
                {
                    Number = reader.GetInt32(0),
                    Kl1 = reader.GetDecimal(1),
                    Kl2 = reader.GetDecimal(2),
                    Kf1 = reader.GetDecimal(3),
                    Kf2 = reader.GetDecimal(4),
                    Kf3 = reader.GetDecimal(5),
                    Kf4 = reader.GetDecimal(6),
                });
            }

            snapshot.Coeffs = coeffs.ToArray();
        }

        using (var thermoCommand = connection.CreateCommand())
        {
            thermoCommand.CommandText = """
                SELECT block_number, shift_index, shift_label, hours, dro, pwd, tpw,
                       tk, top, tpp, pop, tug, thw, o2, tn
                FROM baxta_thermo
                WHERE date = @date
                ORDER BY block_number, shift_index;
                """;
            OptoDatabase.AddParameter(thermoCommand, "@date", dateKey);
            using var reader = thermoCommand.ExecuteReader();
            var thermo = new System.Collections.Generic.List<BaxtaThermoData>();
            while (reader.Read())
            {
                thermo.Add(new BaxtaThermoData
                {
                    BlockNumber = reader.GetInt32(0),
                    ShiftIndex = reader.GetInt32(1),
                    ShiftLabel = reader.GetString(2),
                    Hours = reader.GetInt32(3),
                    Dro = reader.GetInt32(4),
                    Pwd = reader.GetInt32(5),
                    Tpw = reader.GetDecimal(6),
                    Tk = reader.GetDecimal(7),
                    Top = reader.GetDecimal(8),
                    Tpp = reader.GetDecimal(9),
                    Pop = reader.GetDecimal(10),
                    Tug = reader.GetDecimal(11),
                    Thw = reader.GetDecimal(12),
                    O2 = reader.GetDecimal(13),
                    Tn = reader.GetInt32(14),
                });
            }

            snapshot.Thermo = thermo.ToArray();
        }

        using (var plantCommand = connection.CreateCommand())
        {
            plantCommand.CommandText = """
                SELECT urp, urm, tcb1, tcb2, tcb3,
                       wrmn11, wrmn12, wrmn13, wrmn21, wrmn22, wrmn23,
                       wrmn31, wrmn32, wrmn33, wrmn14, wrmn24, wrmn34,
                       setn1k, setn1n, setn2k, setn2n, setn3k, setn3n,
                       setn4k, setn4n, setn5k, setn5n, prises_json
                FROM baxta_plant
                WHERE date = @date;
                """;
            OptoDatabase.AddParameter(plantCommand, "@date", dateKey);
            using var reader = plantCommand.ExecuteReader();
            if (reader.Read())
            {
                snapshot.Plant = new BaxtaPlantData
                {
                    Urp = reader.GetDecimal(0),
                    Urm = reader.GetDecimal(1),
                    Tcb1 = reader.GetDecimal(2),
                    Tcb2 = reader.GetDecimal(3),
                    Tcb3 = reader.GetDecimal(4),
                    Wrmn11 = reader.GetInt32(5),
                    Wrmn12 = reader.GetInt32(6),
                    Wrmn13 = reader.GetInt32(7),
                    Wrmn21 = reader.GetInt32(8),
                    Wrmn22 = reader.GetInt32(9),
                    Wrmn23 = reader.GetInt32(10),
                    Wrmn31 = reader.GetInt32(11),
                    Wrmn32 = reader.GetInt32(12),
                    Wrmn33 = reader.GetInt32(13),
                    Wrmn14 = reader.GetInt32(14),
                    Wrmn24 = reader.GetInt32(15),
                    Wrmn34 = reader.GetInt32(16),
                    Setn1K = reader.GetInt32(17),
                    Setn1N = reader.GetInt32(18),
                    Setn2K = reader.GetInt32(19),
                    Setn2N = reader.GetInt32(20),
                    Setn3K = reader.GetInt32(21),
                    Setn3N = reader.GetInt32(22),
                    Setn4K = reader.GetInt32(23),
                    Setn4N = reader.GetInt32(24),
                    Setn5K = reader.GetInt32(25),
                    Setn5N = reader.GetInt32(26),
                    Prises = JsonSerializer.Deserialize<int[]>(reader.GetString(27), JsonOptions) ?? new int[12],
                };
            }
        }

        return snapshot;
    }

    internal static void SaveResult(SqliteConnection connection, DateTime date, BaxtaReport report)
    {
        var dateKey = OptoDatabase.DateKey(date);
        OptoDatabase.ExecuteNonQuery(connection, "DELETE FROM baxta_result WHERE date = @date;", ("@date", dateKey));
        OptoDatabase.ExecuteNonQuery(connection, """
            INSERT INTO baxta_result (date, calculated_at, urp, urm)
            VALUES (@date, @calculatedAt, @urp, @urm);
            """,
            ("@date", dateKey),
            ("@calculatedAt", OptoDatabase.Timestamp(DateTime.Now)),
            ("@urp", report.Urp),
            ("@urm", report.Urm));
    }
}
