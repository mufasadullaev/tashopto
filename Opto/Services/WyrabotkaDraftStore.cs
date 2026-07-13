using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Opto.Models;

namespace Opto.Services;

public static class WyrabotkaDraftStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private static string DirectoryPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Opto",
            "drafts");

    public static void Save(DateTime date, WyrabotkaMode mode, WyrabotkaEditSnapshot snapshot)
    {
        Directory.CreateDirectory(DirectoryPath);
        var path = FilePath(date);
        var payload = new DraftFile
        {
            Date = date,
            Mode = mode,
            Snapshot = snapshot,
            SavedAt = DateTime.Now,
        };
        File.WriteAllText(path, JsonSerializer.Serialize(payload, JsonOptions));
    }

    public static WyrabotkaEditSnapshot? TryLoad(DateTime date)
    {
        var path = FilePath(date);
        if (!File.Exists(path))
            return null;

        var payload = JsonSerializer.Deserialize<DraftFile>(File.ReadAllText(path), JsonOptions);
        return payload?.Snapshot;
    }

    private static string FilePath(DateTime date) =>
        Path.Combine(DirectoryPath, $"wyrabotka-{date:yyyyMMdd}.json");

    private sealed class DraftFile
    {
        public DateTime Date { get; set; }
        public WyrabotkaMode Mode { get; set; }
        public WyrabotkaEditSnapshot Snapshot { get; set; } = new();
        public DateTime SavedAt { get; set; }
    }
}

public sealed class WyrabotkaEditSnapshot
{
    public BlockDraft[] Blocks { get; set; } = [];
    public TransformerDraft[] Transformers { get; set; } = [];

    public static WyrabotkaEditSnapshot FromRows(
        System.Collections.Generic.IEnumerable<BlockMeterRow> blocks,
        System.Collections.Generic.IEnumerable<TransformerMeterRow> transformers) =>
        new()
        {
            Blocks = blocks.Select(b => new BlockDraft
            {
                Number = b.Number,
                GenerationCoefficient = b.GenerationCoefficient,
                GenerationStart = b.GenerationStart,
                GenerationEnd = b.GenerationEnd,
                OwnNeedsCoefficient = b.OwnNeedsCoefficient,
                OwnNeedsStart = b.OwnNeedsStart,
                OwnNeedsEnd = b.OwnNeedsEnd,
                Hours = b.Hours,
            }).ToArray(),
            Transformers = transformers.Select(t => new TransformerDraft
            {
                Name = t.Name,
                Coefficient = t.Coefficient,
                Start = t.Start,
                End = t.End,
            }).ToArray(),
        };

    public void ApplyTo(
        System.Collections.Generic.IList<BlockMeterRow> blocks,
        System.Collections.Generic.IList<TransformerMeterRow> transformers)
    {
        foreach (var draft in Blocks)
        {
            var row = blocks.FirstOrDefault(b => b.Number == draft.Number);
            if (row is null)
                continue;

            row.GenerationCoefficient = draft.GenerationCoefficient;
            row.GenerationStart = draft.GenerationStart;
            row.GenerationEnd = draft.GenerationEnd;
            row.OwnNeedsCoefficient = draft.OwnNeedsCoefficient;
            row.OwnNeedsStart = draft.OwnNeedsStart;
            row.OwnNeedsEnd = draft.OwnNeedsEnd;
            row.Hours = draft.Hours != 0
                ? draft.Hours
                : draft.GenerationHours != 0
                    ? draft.GenerationHours
                    : draft.OwnNeedsHours;
        }

        foreach (var draft in Transformers)
        {
            var row = transformers.FirstOrDefault(t => t.Name == draft.Name);
            if (row is null)
                continue;

            row.Coefficient = draft.Coefficient;
            row.Start = draft.Start;
            row.End = draft.End;
        }
    }
}

public sealed class BlockDraft
{
    public int Number { get; set; }
    public decimal GenerationCoefficient { get; set; }
    public decimal GenerationStart { get; set; }
    public decimal GenerationEnd { get; set; }
    public decimal OwnNeedsCoefficient { get; set; }
    public decimal OwnNeedsStart { get; set; }
    public decimal OwnNeedsEnd { get; set; }
    public int Hours { get; set; }

    // Старые черновики
    public int GenerationHours { get; set; }
    public int OwnNeedsHours { get; set; }
}

public sealed class TransformerDraft
{
    public string Name { get; set; } = "";
    public decimal Coefficient { get; set; }
    public decimal Start { get; set; }
    public decimal End { get; set; }
}
