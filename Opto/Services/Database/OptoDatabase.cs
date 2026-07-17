using System;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Opto.Services.Database;

public static class OptoDatabase
{
    public const int SchemaVersion = 4;

    public static string DatabasePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Opto",
            "opto.db");

    public static void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        ExecuteNonQuery(connection, """
            CREATE TABLE IF NOT EXISTS schema_version (
                version INTEGER NOT NULL
            );
            """);

        var version = GetSchemaVersion(connection);
        if (version == 0)
        {
            CreateSchema(connection);
            ExecuteNonQuery(connection, "INSERT INTO schema_version (version) VALUES (@version);",
                ("@version", SchemaVersion));
        }
        else if (version < SchemaVersion)
        {
            UpgradeSchema(connection, version);
            ExecuteNonQuery(connection, "UPDATE schema_version SET version = @version;",
                ("@version", SchemaVersion));
        }

        transaction.Commit();
    }

    private static void UpgradeSchema(SqliteConnection connection, int fromVersion)
    {
        if (fromVersion < 2)
            RebuildBaxtaPlantWithoutSetn(connection);
        if (fromVersion < 3)
            AddSetnToBaxtaPlant(connection);
        if (fromVersion < 4)
            CreatePerejegSchema(connection);
    }

    private static void CreatePerejegSchema(SqliteConnection connection)
    {
        if (TableExists(connection, "perejeg_day"))
            return;

        ExecuteNonQuery(connection, """
            CREATE TABLE perejeg_day (
                date TEXT PRIMARY KEY,
                mode INTEGER NOT NULL,
                saved_at TEXT NOT NULL
            );

            CREATE TABLE perejeg_row (
                date TEXT NOT NULL,
                row_index INTEGER NOT NULL,
                block1 REAL NOT NULL,
                block2 REAL NOT NULL,
                block3 REAL NOT NULL,
                block4 REAL NOT NULL,
                block5 REAL NOT NULL,
                block6 REAL NOT NULL,
                block7 REAL NOT NULL,
                block8 REAL NOT NULL,
                block9 REAL NOT NULL,
                block10 REAL NOT NULL,
                block11 REAL NOT NULL,
                block12 REAL NOT NULL,
                station REAL NOT NULL,
                PRIMARY KEY (date, row_index),
                FOREIGN KEY (date) REFERENCES perejeg_day(date) ON DELETE CASCADE
            );

            CREATE TABLE perejeg_result (
                date TEXT PRIMARY KEY,
                calculated_at TEXT NOT NULL,
                release_mln_kwh REAL NOT NULL,
                total_gkwh REAL NOT NULL,
                result_json TEXT NOT NULL,
                FOREIGN KEY (date) REFERENCES perejeg_day(date) ON DELETE CASCADE
            );
            """);
    }

    private static void AddSetnToBaxtaPlant(SqliteConnection connection)
    {
        if (!TableExists(connection, "baxta_plant"))
            return;

        if (ColumnExists(connection, "baxta_plant", "setn1k"))
            return;

        foreach (var column in new[]
                 {
                     "setn1k", "setn1n", "setn2k", "setn2n", "setn3k", "setn3n",
                     "setn4k", "setn4n", "setn5k", "setn5n",
                 })
        {
            ExecuteNonQuery(connection, $"ALTER TABLE baxta_plant ADD COLUMN {column} INTEGER NOT NULL DEFAULT 0;");
        }
    }

    private static void RebuildBaxtaPlantWithoutSetn(SqliteConnection connection)
    {
        if (!TableExists(connection, "baxta_plant"))
            return;

        if (!ColumnExists(connection, "baxta_plant", "setn1k"))
            return;

        ExecuteNonQuery(connection, """
            CREATE TABLE baxta_plant_new (
                date TEXT PRIMARY KEY,
                urp REAL NOT NULL,
                urm REAL NOT NULL,
                tcb1 REAL NOT NULL,
                tcb2 REAL NOT NULL,
                tcb3 REAL NOT NULL,
                wrmn11 INTEGER NOT NULL,
                wrmn12 INTEGER NOT NULL,
                wrmn13 INTEGER NOT NULL,
                wrmn21 INTEGER NOT NULL,
                wrmn22 INTEGER NOT NULL,
                wrmn23 INTEGER NOT NULL,
                wrmn31 INTEGER NOT NULL,
                wrmn32 INTEGER NOT NULL,
                wrmn33 INTEGER NOT NULL,
                wrmn14 INTEGER NOT NULL,
                wrmn24 INTEGER NOT NULL,
                wrmn34 INTEGER NOT NULL,
                prises_json TEXT NOT NULL,
                FOREIGN KEY (date) REFERENCES baxta_day(date) ON DELETE CASCADE
            );

            INSERT INTO baxta_plant_new (
                date, urp, urm, tcb1, tcb2, tcb3,
                wrmn11, wrmn12, wrmn13, wrmn21, wrmn22, wrmn23,
                wrmn31, wrmn32, wrmn33, wrmn14, wrmn24, wrmn34,
                prises_json)
            SELECT
                date, urp, urm, tcb1, tcb2, tcb3,
                wrmn11, wrmn12, wrmn13, wrmn21, wrmn22, wrmn23,
                wrmn31, wrmn32, wrmn33, wrmn14, wrmn24, wrmn34,
                prises_json
            FROM baxta_plant;

            DROP TABLE baxta_plant;
            ALTER TABLE baxta_plant_new RENAME TO baxta_plant;
            """);
    }

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = @name LIMIT 1;";
        AddParameter(command, "@name", tableName);
        return command.ExecuteScalar() is not null;
    }

    private static bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();
        ExecuteNonQuery(connection, "PRAGMA foreign_keys = ON;");
        return connection;
    }

    private static int GetSchemaVersion(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT version FROM schema_version LIMIT 1;";
        var value = command.ExecuteScalar();
        return value is null or DBNull ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static void CreateSchema(SqliteConnection connection)
    {
        ExecuteNonQuery(connection, """
            CREATE TABLE wyrabotka_day (
                date TEXT PRIMARY KEY,
                mode INTEGER NOT NULL,
                saved_at TEXT NOT NULL
            );

            CREATE TABLE wyrabotka_block (
                date TEXT NOT NULL,
                number INTEGER NOT NULL,
                generation_coefficient REAL NOT NULL,
                generation_start REAL NOT NULL,
                generation_end REAL NOT NULL,
                own_needs_coefficient REAL NOT NULL,
                own_needs_start REAL NOT NULL,
                own_needs_end REAL NOT NULL,
                hours INTEGER NOT NULL,
                PRIMARY KEY (date, number),
                FOREIGN KEY (date) REFERENCES wyrabotka_day(date) ON DELETE CASCADE
            );

            CREATE TABLE wyrabotka_transformer (
                date TEXT NOT NULL,
                name TEXT NOT NULL,
                coefficient REAL NOT NULL,
                start_value REAL NOT NULL,
                end_value REAL NOT NULL,
                PRIMARY KEY (date, name),
                FOREIGN KEY (date) REFERENCES wyrabotka_day(date) ON DELETE CASCADE
            );

            CREATE TABLE wyrabotka_result (
                date TEXT PRIMARY KEY,
                day_hours INTEGER NOT NULL,
                day_generation REAL NOT NULL,
                day_own_needs REAL NOT NULL,
                day_release REAL NOT NULL,
                calculated_at TEXT NOT NULL,
                FOREIGN KEY (date) REFERENCES wyrabotka_day(date) ON DELETE CASCADE
            );

            CREATE TABLE baxta_day (
                date TEXT PRIMARY KEY,
                mode INTEGER NOT NULL,
                saved_at TEXT NOT NULL
            );

            CREATE TABLE baxta_meter (
                date TEXT NOT NULL,
                number INTEGER NOT NULL,
                generation_coefficient REAL NOT NULL,
                generation_at0 REAL NOT NULL,
                generation_at8 REAL NOT NULL,
                generation_at16 REAL NOT NULL,
                generation_at24 REAL NOT NULL,
                own_needs_coefficient REAL NOT NULL,
                own_needs_at0 REAL NOT NULL,
                own_needs_at8 REAL NOT NULL,
                own_needs_at16 REAL NOT NULL,
                own_needs_at24 REAL NOT NULL,
                PRIMARY KEY (date, number),
                FOREIGN KEY (date) REFERENCES baxta_day(date) ON DELETE CASCADE
            );

            CREATE TABLE baxta_coeff (
                date TEXT NOT NULL,
                number INTEGER NOT NULL,
                kl1 REAL NOT NULL,
                kl2 REAL NOT NULL,
                kf1 REAL NOT NULL,
                kf2 REAL NOT NULL,
                kf3 REAL NOT NULL,
                kf4 REAL NOT NULL,
                PRIMARY KEY (date, number),
                FOREIGN KEY (date) REFERENCES baxta_day(date) ON DELETE CASCADE
            );

            CREATE TABLE baxta_thermo (
                date TEXT NOT NULL,
                block_number INTEGER NOT NULL,
                shift_index INTEGER NOT NULL,
                shift_label TEXT NOT NULL,
                hours INTEGER NOT NULL,
                dro INTEGER NOT NULL,
                pwd INTEGER NOT NULL,
                tpw REAL NOT NULL,
                tk REAL NOT NULL,
                top REAL NOT NULL,
                tpp REAL NOT NULL,
                pop REAL NOT NULL,
                tug REAL NOT NULL,
                thw REAL NOT NULL,
                o2 REAL NOT NULL,
                tn INTEGER NOT NULL,
                PRIMARY KEY (date, block_number, shift_index),
                FOREIGN KEY (date) REFERENCES baxta_day(date) ON DELETE CASCADE
            );

            CREATE TABLE baxta_plant (
                date TEXT PRIMARY KEY,
                urp REAL NOT NULL,
                urm REAL NOT NULL,
                tcb1 REAL NOT NULL,
                tcb2 REAL NOT NULL,
                tcb3 REAL NOT NULL,
                wrmn11 INTEGER NOT NULL,
                wrmn12 INTEGER NOT NULL,
                wrmn13 INTEGER NOT NULL,
                wrmn21 INTEGER NOT NULL,
                wrmn22 INTEGER NOT NULL,
                wrmn23 INTEGER NOT NULL,
                wrmn31 INTEGER NOT NULL,
                wrmn32 INTEGER NOT NULL,
                wrmn33 INTEGER NOT NULL,
                wrmn14 INTEGER NOT NULL,
                wrmn24 INTEGER NOT NULL,
                wrmn34 INTEGER NOT NULL,
                setn1k INTEGER NOT NULL,
                setn1n INTEGER NOT NULL,
                setn2k INTEGER NOT NULL,
                setn2n INTEGER NOT NULL,
                setn3k INTEGER NOT NULL,
                setn3n INTEGER NOT NULL,
                setn4k INTEGER NOT NULL,
                setn4n INTEGER NOT NULL,
                setn5k INTEGER NOT NULL,
                setn5n INTEGER NOT NULL,
                prises_json TEXT NOT NULL,
                FOREIGN KEY (date) REFERENCES baxta_day(date) ON DELETE CASCADE
            );

            CREATE TABLE baxta_result (
                date TEXT PRIMARY KEY,
                calculated_at TEXT NOT NULL,
                urp REAL NOT NULL,
                urm REAL NOT NULL,
                FOREIGN KEY (date) REFERENCES baxta_day(date) ON DELETE CASCADE
            );

            CREATE TABLE perejeg_day (
                date TEXT PRIMARY KEY,
                mode INTEGER NOT NULL,
                saved_at TEXT NOT NULL
            );

            CREATE TABLE perejeg_row (
                date TEXT NOT NULL,
                row_index INTEGER NOT NULL,
                block1 REAL NOT NULL,
                block2 REAL NOT NULL,
                block3 REAL NOT NULL,
                block4 REAL NOT NULL,
                block5 REAL NOT NULL,
                block6 REAL NOT NULL,
                block7 REAL NOT NULL,
                block8 REAL NOT NULL,
                block9 REAL NOT NULL,
                block10 REAL NOT NULL,
                block11 REAL NOT NULL,
                block12 REAL NOT NULL,
                station REAL NOT NULL,
                PRIMARY KEY (date, row_index),
                FOREIGN KEY (date) REFERENCES perejeg_day(date) ON DELETE CASCADE
            );

            CREATE TABLE perejeg_result (
                date TEXT PRIMARY KEY,
                calculated_at TEXT NOT NULL,
                release_mln_kwh REAL NOT NULL,
                total_gkwh REAL NOT NULL,
                result_json TEXT NOT NULL,
                FOREIGN KEY (date) REFERENCES perejeg_day(date) ON DELETE CASCADE
            );
            """);

        ExecuteNonQuery(connection, "PRAGMA foreign_keys = ON;");
    }

    internal static string DateKey(DateTime date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    internal static string Timestamp(DateTime value) =>
        value.ToString("O", CultureInfo.InvariantCulture);

    internal static DateTime ParseTimestamp(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    internal static void ExecuteNonQuery(
        SqliteConnection connection,
        string sql,
        params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            AddParameter(command, name, value);
        command.ExecuteNonQuery();
    }

    internal static void AddParameter(SqliteCommand command, string name, object? value)
    {
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }

    internal static decimal ReadDecimal(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? 0m : reader.GetDecimal(ordinal);

    internal static int ReadInt(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
}
