using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Opto.Models;
using Opto.Services.Database;

namespace Opto.Services;

public static class OptoTestRunner
{
    public static void RunAllTests()
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("       ЗАПУСК ПОЛНОГО ТЕСТИРОВАНИЯ OPTO         ");
        Console.WriteLine("=================================================");

        var passed = 0;
        var failed = 0;

        void Test(string name, Action action)
        {
            try
            {
                Console.Write($"[ТЕСТ] {name} ... ");
                action();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("УСПЕШНО ✅");
                Console.ResetColor();
                passed++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ОШИБКА ❌ ({ex.Message})");
                Console.ResetColor();
                failed++;
            }
        }

        Test("Инициализация базы данных SQLite", () =>
        {
            OptoDatabase.Initialize();
            using var connection = OptoDatabase.OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            var result = cmd.ExecuteScalar()?.ToString();
            if (result != "ok")
                throw new Exception($"Integrity check failed: {result}");
        });

        Test("Модуль Выработка (Wyrabotka) — расчёт и сохранность", () =>
        {
            var testDate = new DateTime(2026, 7, 14);
            var blocks = new List<BlockMeterRow>();
            for (var i = 1; i <= 12; i++)
            {
                blocks.Add(new BlockMeterRow
                {
                    Number = i,
                    GenerationCoefficient = 2880,
                    GenerationStart = 1000m,
                    GenerationEnd = 1100m, // Delta = 100 -> 288 тыс. кВт·ч
                    OwnNeedsCoefficient = 7200,
                    OwnNeedsStart = 500m,
                    OwnNeedsEnd = 505m, // Delta = 5 -> 36 тыс. кВт·ч
                    Hours = 24,
                });
            }

            var transformers = new List<TransformerMeterRow>
            {
                new() { Name = "ТС-20", Coefficient = 2400, Start = 100m, End = 101m } // Delta = 1 -> 2.4 тыс. кВт·ч
            };

            var report = WyrabotkaCalculator.Calculate(testDate, WyrabotkaMode.Calculation, blocks, transformers);
            if (report.DayTotal.GenerationThousandKwh != 3456.0m)
                throw new Exception($"Неверная выработка: {report.DayTotal.GenerationThousandKwh}");

            var snapshot = WyrabotkaEditSnapshot.FromRows(blocks, transformers);
            WyrabotkaStore.Save(testDate, WyrabotkaMode.Calculation, snapshot);
            WyrabotkaStore.SaveResult(testDate, report);

            var loadedResult = WyrabotkaStore.TryLoadResult(testDate);
            if (loadedResult is null)
                throw new Exception("Не удалось загрузить результат Выработки из БД");
        });

        Test("Модуль Вахта (Baxta) — эталонная математика (блок 2, смена 1)", () =>
        {
            var testDate = new DateTime(2026, 7, 14);

            var meters = new List<BaxtaMeterBlockRow>();
            for (var i = 1; i <= 12; i++)
            {
                meters.Add(new BaxtaMeterBlockRow
                {
                    Number = i,
                    GenerationCoefficient = 2880,
                    GenerationAt0 = i == 2 ? 799361.9m : 1000m,
                    GenerationAt8 = i == 2 ? 799609.1m : 1000m,
                    GenerationAt16 = i == 2 ? 799609.1m : 1000m,
                    GenerationAt24 = i == 2 ? 799609.1m : 1000m,
                    OwnNeedsCoefficient = 7200,
                    OwnNeedsAt0 = i == 2 ? 3767.52m : 500m,
                    OwnNeedsAt8 = i == 2 ? 3774.01m : 500m,
                    OwnNeedsAt16 = 500m,
                    OwnNeedsAt24 = 500m,
                });
            }

            var thermo = new List<BaxtaThermoRow>();
            for (var b = 1; b <= 12; b++)
            {
                for (var s = 1; s <= 3; s++)
                {
                    var isTarget = b == 2 && s == 1;
                    thermo.Add(new BaxtaThermoRow
                    {
                        BlockNumber = b,
                        ShiftIndex = s,
                        ShiftLabel = s == 1 ? "0–8 ч" : s == 2 ? "8–16 ч" : "16–24 ч",
                        Hours = isTarget ? 8 : 0,
                        Tn = isTarget ? 6846 : 0,
                        Dro = isTarget ? 11 : 0,
                        Pwd = isTarget ? 1 : 0,
                        Tpw = isTarget ? 205.0m : 0m,
                        Tk = isTarget ? 26.3m : 0m,
                        Top = isTarget ? 543.0m : 0m,
                        Tpp = isTarget ? 543.0m : 0m,
                        Pop = isTarget ? 77.0m : 0m,
                        Tug = isTarget ? 148.0m : 0m,
                        Thw = isTarget ? 34.0m : 0m,
                        O2 = isTarget ? 4.4m : 0m,
                    });
                }
            }

            var plant = new BaxtaPlantParamsRow
            {
                Urp = 320,
                Urm = 310,
                Tcb1 = 14.1m,
                Tcb2 = 14.1m,
                Tcb3 = 14.1m,
            };
            for (var p = 0; p < 12; p++) plant.Prises[p] = 39;

            var coeffs = new List<BaxtaBlockCoeffsRow>();
            for (var i = 1; i <= 12; i++)
            {
                coeffs.Add(new BaxtaBlockCoeffsRow
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

            var (report, details) = BaxtaCalculator.CalculateWithDetails(
                testDate, WyrabotkaMode.Calculation, meters, thermo, plant, coeffs, [1, 2, 3]);

            var b2s1 = details.FirstOrDefault(d => d.BlockNumber == 2 && d.ShiftIndex == 1);
            if (b2s1 is null)
                throw new Exception("Детализация по блоку 2 не найдена.");

            if (Math.Abs(b2s1.Load - 88.9875) > 0.1)
                throw new Exception($"Нагрузка не совпадает: {b2s1.Load}");

            if (Math.Abs(b2s1.Pug - 5586) > 5)
                throw new Exception($"ПУГ не совпадает: {b2s1.Pug}");

            if (Math.Abs(b2s1.Wak - 1194) > 5)
                throw new Exception($"ВАК не совпадает: {b2s1.Wak}");

            if (Math.Abs(b2s1.Dop - 947) > 5)
                throw new Exception($"ДОП не совпадает: {b2s1.Dop}");

            if (Math.Abs(b2s1.Top - (-150)) > 5)
                throw new Exception($"ТОП не совпадает: {b2s1.Top}");

            if (Math.Abs(b2s1.Sn - 751) > 5)
                throw new Exception($"СН не совпадает: {b2s1.Sn}");

            if (Math.Abs(b2s1.Tpw - (-165)) > 5)
                throw new Exception($"ТПВ не совпадает: {b2s1.Tpw}");

            var snapshot = BaxtaEditSnapshot.FromRows(meters, coeffs, thermo, plant);
            BaxtaStore.Save(testDate, WyrabotkaMode.Calculation, snapshot);
            BaxtaStore.SaveCalculationResult(testDate, report, details);
        });

        Test("Умное автозаполнение — перенос 24:00 строго в 0:00 следующего дня", () =>
        {
            var today = new DateTime(2026, 7, 14);
            var tomorrow = new DateTime(2026, 7, 15);

            using (var connection = OptoDatabase.OpenConnection())
            {
                OptoDatabase.ExecuteNonQuery(connection, "DELETE FROM baxta_day WHERE date = @date;", ("@date", OptoDatabase.DateKey(tomorrow)));
            }

            var template = BaxtaStore.TryLoadTemplateSnapshot(tomorrow);
            if (template is null)
                throw new Exception("Автозаполнение не сформировало шаблон");

            var b2 = template.Meters.First(m => m.Number == 2);
            if (b2.GenerationAt0 != 799609.1m)
                throw new Exception($"Показание 0:00 должно быть 799609.1, а получено {b2.GenerationAt0}");

            if (b2.GenerationAt8 != 0m || b2.GenerationAt16 != 0m || b2.GenerationAt24 != 0m)
                throw new Exception("Показания 8:00, 16:00, 24:00 должны быть равны 0");
        });

        Test("Модуль Пережоги (Perejeg) — формулы отклонений", () =>
        {
            var testDate = new DateTime(2026, 7, 14);
            var rows = PerejegInputFactory.CreateEmptyRows();

            // Задаём Nrvv (строка 1) = 10 МВт, Trvv (строка 8) = 5 часов для блока 1
            rows[0].Block1 = 10m;
            rows[7].Block1 = 5m;
            rows[15].Station = 100m; // Ej = 100 млн кВт·ч

            var report = PerejegCalculator.Calculate(testDate, WyrabotkaMode.Calculation, rows);
            // 1 РВВ: 10.6 * 10 * 5 = 530 кг у.т.
            var row1Cell1 = report.Rows[0].Col1;
            if (row1Cell1 != "530")
                throw new Exception($"Неверный расчёт пережога 1 РВВ: {row1Cell1}");
        });

        Test("Модуль Сводный расчёт (O2Gol) — агрегация отчётов", () =>
        {
            var testDate = new DateTime(2026, 7, 14);
            var details = BaxtaStore.HasResult(testDate)
                ? BaxtaCalculator.CalculateShiftDetails(testDate, BaxtaStore.TryLoad(testDate)!, [1, 2, 3])
                : [];

            var report = O2GolCalculator.Build(testDate, testDate, details);
            if (report.ByBlocks.Rows.Count != 11)
                throw new Exception($"Неверное количество строк в O2Gol: {report.ByBlocks.Rows.Count}");
        });

        Test("Модуль Акт выработки (Akt) — валовая выработка и полезный отпуск", () =>
        {
            var testDate = new DateTime(2026, 7, 14);
            var monthWyr = WyrabotkaStore.TryLoadResult(testDate);

            var input = new AktInputSnapshot
            {
                Date = testDate,
                ReserveExciterKwh = 1000m,
                LossesKwh = 500m,
                PlantFacilitiesKwh = 200m,
                PreventoriumKwh = 100m,
            };

            var report = AktCalculator.Calculate(testDate, monthWyr, input);
            if (report.ReserveExciterKwh != 1000m)
                throw new Exception($"Ошибка в акте: {report.ReserveExciterKwh}");
        });

        Test("Модуль Селекторный расчёт (Selektor) — утренняя сводка", () =>
        {
            var testDate = new DateTime(2026, 7, 14);
            var monthWyr = WyrabotkaStore.TryLoadResult(testDate);
            var report = SelektorCalculator.Calculate(testDate, monthWyr);
            if (report.Blocks.Count != 12)
                throw new Exception($"Количество блоков в селекторе != 12: {report.Blocks.Count}");
        });

        Test("Экспорт отчётов (ExportService) — генерация CSV с BOM", () =>
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "test_opto_export.csv");
            var headers = new[] { "Колонка1", "Колонка2" };
            var rows = new List<List<string>> { new() { "Значение 1", "Значение 2" } };

            ExportService.ExportToCsvAsync(tempPath, headers, rows).Wait();

            if (!File.Exists(tempPath))
                throw new Exception("Файл CSV не создан.");

            var text = File.ReadAllText(tempPath);
            if (!text.Contains("Значение 1"))
                throw new Exception("Содержимое CSV некорректно.");

            File.Delete(tempPath);
        });

        Console.WriteLine("=================================================");
        Console.WriteLine($" РЕЗУЛЬТАТ: Пройдено {passed} из {passed + failed} тестов. ");
        Console.WriteLine("=================================================");

        if (failed > 0)
            throw new Exception($"Обнаружено {failed} ошибок в модулях.");
    }
}
