using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Opto.Models;
using Opto.Services.Database;

namespace Opto.Services;

public static class AktStore
{
    public static void Save(DateTime date, AktInputSnapshot snapshot)
    {
        using var connection = OptoDatabase.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var dateKey = OptoDatabase.DateKey(date);

        OptoDatabase.ExecuteNonQuery(connection, """
            CREATE TABLE IF NOT EXISTS akt_data (
                date TEXT PRIMARY KEY,
                json_data TEXT NOT NULL,
                saved_at TEXT NOT NULL
            );
            """);

        OptoDatabase.ExecuteNonQuery(connection, """
            INSERT INTO akt_data (date, json_data, saved_at)
            VALUES (@date, @json, @savedAt)
            ON CONFLICT(date) DO UPDATE SET
                json_data = @json,
                saved_at = @savedAt;
            """,
            ("@date", dateKey),
            ("@json", JsonSerializer.Serialize(snapshot)),
            ("@savedAt", OptoDatabase.Timestamp(DateTime.Now)));

        transaction.Commit();
    }

    public static AktInputSnapshot? TryLoad(DateTime date)
    {
        using var connection = OptoDatabase.OpenConnection();
        var dateKey = OptoDatabase.DateKey(date);

        OptoDatabase.ExecuteNonQuery(connection, """
            CREATE TABLE IF NOT EXISTS akt_data (
                date TEXT PRIMARY KEY,
                json_data TEXT NOT NULL,
                saved_at TEXT NOT NULL
            );
            """);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT json_data FROM akt_data WHERE date = @date LIMIT 1;";
        OptoDatabase.AddParameter(command, "@date", dateKey);

        var result = command.ExecuteScalar();
        if (result is string json)
        {
            try
            {
                return JsonSerializer.Deserialize<AktInputSnapshot>(json);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }
}
