using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Opto.Models;

namespace Opto.Services;

public static class WyrabotkaPrintService
{
    private static readonly CultureInfo Culture = OptoCulture.Current;

    public static void Print(WyrabotkaReport report)
    {
        var html = BuildHtml(report);
        var path = Path.Combine(
            Path.GetTempPath(),
            $"opto-wyrabotka-{report.Date:yyyyMMdd}-{DateTime.Now:HHmmss}.html");

        File.WriteAllText(path, html, Encoding.UTF8);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        });
    }

    private static string BuildHtml(WyrabotkaReport report)
    {
        var trSum = report.TransformerRows.Sum(r => r.OwnNeedsThousandKwh);
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"ru\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\" />");
        sb.AppendLine($"<title>ОПТО · {report.Date:dd.MM.yyyy}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("""
            @page { size: A4 landscape; margin: 10mm 12mm; }

            * { box-sizing: border-box; }

            body {
              margin: 0;
              padding: 12px 16px;
              color: #111;
              background: #fff;
              font: 11px/1.3 "Segoe UI", "Helvetica Neue", Arial, sans-serif;
              -webkit-print-color-adjust: exact;
              print-color-adjust: exact;
            }
            """);
        sb.AppendLine(OptoPrintChrome.SharedStyles);
        sb.AppendLine("""
            h2 {
              margin: 0 0 4px;
              font-size: 10px;
              font-weight: 700;
              letter-spacing: 0.06em;
              text-transform: uppercase;
              color: #555;
            }

            table {
              width: 100%;
              border-collapse: collapse;
              table-layout: fixed;
            }

            th, td {
              padding: 3px 5px;
              border-bottom: 1px solid #ddd;
              font-variant-numeric: tabular-nums;
              vertical-align: middle;
            }

            th {
              font-size: 9.5px;
              font-weight: 600;
              color: #555;
              text-align: right;
              border-bottom: 1px solid #222;
            }

            th.l, td.l { text-align: left; }
            th.c, td.c { text-align: center; }
            td { text-align: right; }
            td.l { font-weight: 600; }

            thead .g th {
              text-align: center;
              border-bottom: none;
              padding-bottom: 1px;
              color: #333;
            }

            .w-n { width: 32px; }
            .w-k { width: 52px; }
            .w-h { width: 44px; }
            .w-p { width: 48px; }

            tr.tot td {
              border-top: 1px solid #222;
              border-bottom: none;
              font-weight: 700;
              padding-top: 5px;
            }
            tr.rel td {
              border-bottom: none;
              font-weight: 700;
            }

            .bottom {
              display: grid;
              grid-template-columns: 1fr 1.4fr;
              gap: 18px;
              margin-top: 14px;
              align-items: start;
            }

            .foot {
              margin-top: 10px;
              padding-top: 4px;
              border-top: 1px solid #ddd;
              display: flex;
              justify-content: space-between;
              color: #777;
              font-size: 9.5px;
            }

            @media print {
              body { padding: 0; }
            }
            """);
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body onload=\"setTimeout(() => window.print(), 200)\">");

        OptoPrintChrome.AppendToolbar(sb);
        OptoPrintChrome.AppendHeader(
            sb,
            "РАСЧЁТ ВЫРАБОТКИ ЭЛЕКТРОЭНЕРГИИ И ПОТРЕБЛЕНИЯ ЕЕ НА СОБСТВЕННЫЕ НУЖДЫ",
            report.Date,
            report.Mode,
            "Выработка электроэнергии");

        // Blocks — full width
        sb.AppendLine("<section>");
        sb.AppendLine("<h2>Блоки</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead>");
        sb.AppendLine("<tr class=\"g\">");
        sb.AppendLine("<th class=\"c w-n\" rowspan=\"2\">№</th>");
        sb.AppendLine("<th class=\"c w-h\" rowspan=\"2\">Час</th>");
        sb.AppendLine("<th colspan=\"3\">Выработка</th>");
        sb.AppendLine("<th colspan=\"3\">СН</th>");
        sb.AppendLine("<th class=\"w-p\" rowspan=\"2\">%</th>");
        sb.AppendLine("</tr>");
        sb.AppendLine("<tr>");
        sb.AppendLine("<th class=\"c\">КФЦ</th><th>тыс.кВт·ч</th><th>МВт</th>");
        sb.AppendLine("<th class=\"c\">КФЦ</th><th>тыс.кВт·ч</th><th>МВт</th>");
        sb.AppendLine("</tr>");
        sb.AppendLine("</thead><tbody>");

        foreach (var row in report.BlockRows)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td class=\"c\">{Esc(row.Label)}</td>");
            sb.AppendLine($"<td class=\"c\">{N(row.Hours)}</td>");
            sb.AppendLine($"<td class=\"c\">{N0(row.CoefficientGeneration)}</td>");
            sb.AppendLine($"<td>{N1(row.GenerationThousandKwh)}</td>");
            sb.AppendLine($"<td>{N1(row.GenerationMw)}</td>");
            sb.AppendLine($"<td class=\"c\">{N0(row.CoefficientOwnNeeds)}</td>");
            sb.AppendLine($"<td>{N1(row.OwnNeedsThousandKwh)}</td>");
            sb.AppendLine($"<td>{N1(row.OwnNeedsMw)}</td>");
            sb.AppendLine($"<td>{N2(row.Percent)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");
        sb.AppendLine("</section>");

        // Transformers + totals — new row below
        sb.AppendLine("<div class=\"bottom\">");

        sb.AppendLine("<section>");
        sb.AppendLine("<h2>Трансформаторы</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th class=\"l\">Счётчик</th><th class=\"c\">КФЦ</th><th>тыс.кВт·ч</th></tr></thead>");
        sb.AppendLine("<tbody>");
        foreach (var row in report.TransformerRows)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td class=\"l\">{Esc(row.Label)}</td>");
            sb.AppendLine($"<td class=\"c\">{N0(row.CoefficientOwnNeeds)}</td>");
            sb.AppendLine($"<td>{N1(row.OwnNeedsThousandKwh)}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("<tr class=\"tot\">");
        sb.AppendLine("<td class=\"l\">Итого</td>");
        sb.AppendLine("<td></td>");
        sb.AppendLine($"<td>{N1(trSum)}</td>");
        sb.AppendLine("</tr>");
        sb.AppendLine("</tbody></table>");
        sb.AppendLine("</section>");

        sb.AppendLine("<section>");
        sb.AppendLine("<h2>Итоги</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine("<th class=\"l\"></th>");
        sb.AppendLine("<th class=\"c w-h\">Час</th>");
        sb.AppendLine("<th>Выр.</th><th>МВт</th>");
        sb.AppendLine("<th>СН+тр.</th><th class=\"w-p\">%</th>");
        sb.AppendLine("</tr></thead><tbody>");

        AppendSummary(sb, report.DayTotal, report.DayRelease);
        AppendSummary(sb, report.MonthTotal, report.MonthRelease);

        sb.AppendLine("</tbody></table>");
        sb.AppendLine("</section>");

        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"foot\">");
        sb.AppendLine("<span>ОПТО</span>");
        sb.AppendLine($"<span>{DateTime.Now:dd.MM.yyyy HH:mm}</span>");
        sb.AppendLine("</div>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static void AppendSummary(StringBuilder sb, WyrabotkaReportRow total, decimal release)
    {
        sb.AppendLine("<tr class=\"tot\">");
        sb.AppendLine($"<td class=\"l\">{Esc(total.Label)}</td>");
        sb.AppendLine($"<td class=\"c\">{N(total.Hours)}</td>");
        sb.AppendLine($"<td>{N1(total.GenerationThousandKwh)}</td>");
        sb.AppendLine($"<td>{N1(total.GenerationMw)}</td>");
        sb.AppendLine($"<td>{N1(total.OwnNeedsThousandKwh)}</td>");
        sb.AppendLine($"<td>{N2(total.Percent)}</td>");
        sb.AppendLine("</tr>");
        sb.AppendLine("<tr class=\"rel\">");
        sb.AppendLine("<td class=\"l\">Отпуск</td>");
        sb.AppendLine("<td></td>");
        sb.AppendLine($"<td>{N2(release)}</td>");
        sb.AppendLine("<td colspan=\"3\"></td>");
        sb.AppendLine("</tr>");
    }

    private static string N(int? value) => value?.ToString(Culture) ?? "—";
    private static string N0(decimal? value) => value?.ToString("0", Culture) ?? "—";
    private static string N1(decimal value) => value.ToString("0.0", Culture);
    private static string N2(decimal value) => value.ToString("0.00", Culture);

    private static string Esc(string? value) =>
        System.Net.WebUtility.HtmlEncode(value ?? "");
}
