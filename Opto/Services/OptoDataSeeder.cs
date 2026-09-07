using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Opto.Models;
using Opto.Services.Database;

namespace Opto.Services;

public static class OptoDataSeeder
{
    public static void ClearAndSeed3Days()
    {
        Console.WriteLine("Очистка базы данных...");

        SqliteConnection.ClearAllPools();
        var dbPath = OptoDatabase.DatabasePath;

        if (File.Exists(dbPath))
        {
            try
            {
                File.Delete(dbPath);
                if (File.Exists(dbPath + "-shm")) File.Delete(dbPath + "-shm");
                if (File.Exists(dbPath + "-wal")) File.Delete(dbPath + "-wal");
            }
            catch
            {
                // Fallback table wipe
            }
        }

        OptoDatabase.Initialize();

        using (var connection = OptoDatabase.OpenConnection())
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                DELETE FROM wyrabotka_result;
                DELETE FROM wyrabotka_transformer;
                DELETE FROM wyrabotka_block;
                DELETE FROM wyrabotka_day;
                DELETE FROM baxta_shift_detail;
                DELETE FROM baxta_result;
                DELETE FROM baxta_plant;
                DELETE FROM baxta_thermo;
                DELETE FROM baxta_coeff;
                DELETE FROM baxta_meter;
                DELETE FROM baxta_day;
                DELETE FROM perejeg_result;
                DELETE FROM perejeg_row;
                DELETE FROM perejeg_day;
                """;
            cmd.ExecuteNonQuery();
        }

        Console.WriteLine("База данных очищена и инициализирована (версия схемы v6).");

        var dates = new[]
        {
            new DateTime(2026, 7, 26),
            new DateTime(2026, 7, 27),
            new DateTime(2026, 7, 28),
        };

        var watchRotations = new[]
        {
            new[] { 1, 2, 3 }, // 26 июля: смены 1=А, 2=Б, 3=В
            new[] { 2, 3, 4 }, // 27 июля: смены 1=Б, 2=В, 3=Г
            new[] { 3, 4, 1 }, // 28 июля: смены 1=В, 2=Г, 3=А
        };

        decimal[,] blockGen24 = new decimal[3, 12];
        decimal[,] blockSn24 = new decimal[3, 12];

        // Default coefficients
        var defaultCoeffs = new List<BaxtaBlockCoeffsRow>();
        for (var i = 1; i <= 12; i++)
        {
            defaultCoeffs.Add(new BaxtaBlockCoeffsRow
            {
                Number = i,
                Kl1 = 0.360m,
                Kl2 = 224m,
                Kf1 = 4.06m,
                Kf2 = 0.017m,
                Kf3 = 5.55m,
                Kf4 = 0.018m,
            });
        }

        for (var dayIdx = 0; dayIdx < dates.Length; dayIdx++)
        {
            var date = dates[dayIdx];
            Console.WriteLine($"Заполнение данных за {date:dd.MM.yyyy}...");

            // --- 1. WYRABOTKA ---
            var wyrBlocks = new List<BlockMeterRow>();
            var genStartBase = 1000m + dayIdx * 200m;
            var genEndBase = genStartBase + 200m + dayIdx * 10m;
            var snStartBase = 500m + dayIdx * 10m;
            var snEndBase = snStartBase + 10m + dayIdx * 1m;

            for (var i = 1; i <= 12; i++)
            {
                wyrBlocks.Add(new BlockMeterRow
                {
                    Number = i,
                    GenerationCoefficient = 2880,
                    GenerationStart = i == 2 ? 799000m + dayIdx * 500m : genStartBase,
                    GenerationEnd = i == 2 ? 799500m + dayIdx * 500m : genEndBase,
                    OwnNeedsCoefficient = 7200,
                    OwnNeedsStart = i == 2 ? 3760m + dayIdx * 15m : snStartBase,
                    OwnNeedsEnd = i == 2 ? 3775m + dayIdx * 15m : snEndBase,
                    Hours = 24,
                });
            }

            var wyrTransformers = new List<TransformerMeterRow>
            {
                new() { Name = "ТС-20", Coefficient = 2400, Start = 100m + dayIdx * 2m, End = 102m + dayIdx * 2m },
                new() { Name = "ТС-30", Coefficient = 2400, Start = 100m + dayIdx * 2m, End = 102m + dayIdx * 2m },
                new() { Name = "3ТР-А", Coefficient = 2400, Start = 100m + dayIdx * 2m, End = 102m + dayIdx * 2m },
                new() { Name = "3ТР-В", Coefficient = 2400, Start = 100m + dayIdx * 2m, End = 102m + dayIdx * 2m },
            };

            var wyrSnapshot = WyrabotkaEditSnapshot.FromRows(wyrBlocks, wyrTransformers);
            WyrabotkaStore.Save(date, WyrabotkaMode.Calculation, wyrSnapshot);
            var monthBefore = WyrabotkaStore.GetMonthTotalsBefore(date);
            var wyrReport = WyrabotkaCalculator.Calculate(date, WyrabotkaMode.Calculation, wyrBlocks, wyrTransformers, monthBefore);
            WyrabotkaStore.SaveResult(date, wyrReport);

            // --- 2. BAXTA ---
            var baxtaMeters = new List<BaxtaMeterBlockRow>();
            for (var i = 1; i <= 12; i++)
            {
                decimal g0, g8, g16, g24;
                decimal s0, s8, s16, s24;

                if (dayIdx == 0)
                {
                    g0 = i == 2 ? 799361.9m : 1000m;
                    s0 = i == 2 ? 3767.52m : 500m;
                }
                else
                {
                    g0 = blockGen24[dayIdx - 1, i - 1];
                    s0 = blockSn24[dayIdx - 1, i - 1];
                }

                g8 = g0 + (i == 2 ? 247.2m : 50m);
                g16 = g8 + (i == 2 ? 240.9m : 50m);
                g24 = g16 + (i == 2 ? 250.0m : 50m);

                s8 = s0 + (i == 2 ? 6.49m : 2.5m);
                s16 = s8 + (i == 2 ? 6.49m : 2.5m);
                s24 = s16 + (i == 2 ? 6.50m : 2.5m);

                blockGen24[dayIdx, i - 1] = g24;
                blockSn24[dayIdx, i - 1] = s24;

                baxtaMeters.Add(new BaxtaMeterBlockRow
                {
                    Number = i,
                    GenerationCoefficient = 2880,
                    GenerationAt0 = g0,
                    GenerationAt8 = g8,
                    GenerationAt16 = g16,
                    GenerationAt24 = g24,
                    OwnNeedsCoefficient = 7200,
                    OwnNeedsAt0 = s0,
                    OwnNeedsAt8 = s8,
                    OwnNeedsAt16 = s16,
                    OwnNeedsAt24 = s24,
                });
            }

            var baxtaThermo = new List<BaxtaThermoRow>();
            for (var b = 1; b <= 12; b++)
            {
                for (var s = 1; s <= 3; s++)
                {
                    var isWorking = b <= 8 || (b == 9 && s == 1);
                    baxtaThermo.Add(new BaxtaThermoRow
                    {
                        BlockNumber = b,
                        ShiftIndex = s,
                        ShiftLabel = s == 1 ? "0–8 ч" : s == 2 ? "8–16 ч" : "16–24 ч",
                        Hours = isWorking ? 8 : 0,
                        Tn = isWorking ? 6800 + b * 10 + s : 0,
                        Dro = isWorking ? 11 : 0,
                        Pwd = isWorking ? 1 : 0,
                        Tpw = isWorking ? 205.0m + dayIdx : 0m,
                        Tk = isWorking ? 26.3m + s * 0.2m : 0m,
                        Top = isWorking ? 543.0m : 0m,
                        Tpp = isWorking ? 543.0m : 0m,
                        Pop = isWorking ? 77.0m : 0m,
                        Tug = isWorking ? 148.0m + dayIdx : 0m,
                        Thw = isWorking ? 34.0m : 0m,
                        O2 = isWorking ? 4.4m : 0m,
                    });
                }
            }

            var plantParams = new BaxtaPlantParamsRow
            {
                Urp = 320m + dayIdx,
                Urm = 310m + dayIdx,
                Tcb1 = 14.1m + dayIdx * 0.2m,
                Tcb2 = 14.5m + dayIdx * 0.2m,
                Tcb3 = 14.2m + dayIdx * 0.2m,
            };
            for (var p = 0; p < 12; p++) plantParams.Prises[p] = 39;

            var baxtaSnapshot = BaxtaEditSnapshot.FromRows(baxtaMeters, defaultCoeffs, baxtaThermo, plantParams);
            BaxtaStore.Save(date, WyrabotkaMode.Calculation, baxtaSnapshot);

            var (baxtaReport, baxtaDetails) = BaxtaCalculator.CalculateWithDetails(
                date, WyrabotkaMode.Calculation, baxtaMeters, baxtaThermo, plantParams, defaultCoeffs, watchRotations[dayIdx]);
            BaxtaStore.SaveCalculationResult(date, baxtaReport, baxtaDetails);

            // --- 3. PEREJEG ---
            var pereRows = PerejegInputFactory.CreateEmptyRows();
            for (var b = 1; b <= 12; b++)
            {
                pereRows[0].SetBlock(b, 10m + dayIdx); // Nrvv
                pereRows[7].SetBlock(b, 4m + dayIdx);  // Trvv
            }
            pereRows[15].Station = 120m + dayIdx * 10m; // Ej

            var pereReport = PerejegCalculator.Calculate(date, WyrabotkaMode.Calculation, pereRows);
            var pereSnapshot = PerejegEditSnapshot.FromRows(pereRows);
            PerejegStore.Save(date, WyrabotkaMode.Calculation, pereSnapshot);
            PerejegStore.SaveResult(date, pereReport);

            // --- 4. AKT & SELEKTOR ---
            var aktInput = new AktInputSnapshot
            {
                Date = date,
                ReserveExciterKwh = 1000m + dayIdx * 100m,
                LossesKwh = 500m,
                PlantFacilitiesKwh = 200m,
                PreventoriumKwh = 100m,
            };
            AktStore.Save(date, aktInput);
        }

        Console.WriteLine("=================================================");
        Console.WriteLine(" Данные на 26, 27, 28 июля 2026 успешно заведены! ");
        Console.WriteLine("=================================================");
    }
}
