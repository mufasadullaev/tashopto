using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Opto.Services;

public static class ExportService
{
    /// <summary>
    /// Exports tabular data to UTF-8 CSV file with BOM so MS Excel opens it cleanly.
    /// </summary>
    public static async Task ExportToCsvAsync(
        string filePath,
        IEnumerable<string> headers,
        IEnumerable<IEnumerable<string>> rows)
    {
        var csvLines = new List<string>
        {
            string.Join(";", headers)
        };

        foreach (var row in rows)
        {
            var escapedCells = new List<string>();
            foreach (var cell in row)
            {
                var text = cell ?? "";
                if (text.Contains(";") || text.Contains("\"") || text.Contains("\n"))
                {
                    text = $"\"{text.Replace("\"", "\"\"")}\"";
                }
                escapedCells.Add(text);
            }
            csvLines.Add(string.Join(";", escapedCells));
        }

        await File.WriteAllLinesAsync(filePath, csvLines, Encoding.UTF8);
    }
}
