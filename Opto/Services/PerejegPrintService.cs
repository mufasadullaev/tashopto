using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Opto.Models;

namespace Opto.Services;

public static class PerejegPrintService
{
    public static void Print(PerejegReport report)
    {
        var html = BuildHtml(report);
        var suffix = report.IsCumulative ? "cumulative" : "daily";
        var path = Path.Combine(
            Path.GetTempPath(),
            $"opto-perejeg-{suffix}-{report.Date:yyyyMMdd}-{DateTime.Now:HHmmss}.html");

        File.WriteAllText(path, html, Encoding.UTF8);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        });
    }

    public static void PrintBlankForm(DateTime? date = null)
    {
        var html = BuildBlankHtml(date);
        var path = Path.Combine(
            Path.GetTempPath(),
            $"opto-perejeg-blank-{DateTime.Now:HHmmss}.html");

        File.WriteAllText(path, html, Encoding.UTF8);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        });
    }

    private static string BuildHtml(PerejegReport report)
    {
        var sb = new StringBuilder();
        var unit = report.DisplayInTons ? "т у.т." : "кг у.т.";

        sb.AppendLine("<!DOCTYPE html><html lang=\"ru\"><head><meta charset=\"utf-8\" />");
        sb.AppendLine($"<title>ОПТО · Пережоги · {report.Date:dd.MM.yyyy}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("""
            @page { size: A4 landscape; margin: 8mm 10mm; }
            body { margin:0; padding:10px 12px; font:9px/1.25 "Segoe UI",Arial,sans-serif; color:#111; }
            """);
        sb.AppendLine(OptoPrintChrome.SharedStyles);
        sb.AppendLine("""
            table { width:100%; border-collapse:collapse; margin-bottom:14px; table-layout:fixed; }
            th, td { border-bottom:1px solid #ddd; padding:2px 3px; text-align:right; }
            th:first-child, td:first-child { text-align:left; }
            .section { font-weight:700; background:#f3f6f8; }
            .footer { margin-top:8px; font-size:10px; }
            """);
        sb.AppendLine("</style></head><body>");
        OptoPrintChrome.AppendToolbar(sb);

        var title = report.IsCumulative
            ? "РАСЧЕТ РАСХОДА ТОПЛИВА ТашТЭС НАРАСТАЮЩИМ ИТОГОМ ПРИ НАЛИЧИИ ИЗМЕНЕНИЙ В СТРУКТУРЕ ТЕПЛОВОЙ СХЕМЫ, СОСТАВЕ И РЕЖИМЕ РАБОТАЮЩЕГО ОБОРУДОВАНИЯ БЛОКОВ"
            : "РАСЧЕТ СУТОЧНОГО РАСХОДА ТОПЛИВА ПРИ НАЛИЧИИ ИЗМЕНЕНИЙ В СТРУКТУРЕ ТЕПЛОВОЙ СХЕМЫ, СОСТАВЕ И РЕЖИМЕ РАБОТАЮЩЕГО ОБОРУДОВАНИЯ БЛОКОВ";

        OptoPrintChrome.AppendHeader(
            sb,
            title,
            report.IsCumulative ? report.PeriodTo ?? report.Date : report.Date,
            report.Mode,
            "Пережоги топлива");

        if (report.IsCumulative && report.PeriodFrom is not null && report.PeriodTo is not null)
        {
            sb.AppendLine(
                $"<p class=\"doc-meta\">Период: {report.PeriodFrom:dd.MM.yyyy} — {report.PeriodTo:dd.MM.yyyy}</p>");
        }

        sb.AppendLine($"<table><caption class=\"section\">Пережоги по типам отклонений, {unit}</caption>");
        sb.AppendLine("<thead><tr><th></th>");
        for (var i = 1; i <= 12; i++)
            sb.AppendLine($"<th>{i}</th>");
        sb.AppendLine("<th>ПО СТАНЦИИ</th></tr></thead><tbody>");

        foreach (var row in report.Rows)
        {
            sb.AppendLine($"<tr><td>{WebUtility.HtmlEncode(row.Label)}</td>");
            sb.AppendLine($"<td>{row.Col1}</td><td>{row.Col2}</td><td>{row.Col3}</td><td>{row.Col4}</td>");
            sb.AppendLine($"<td>{row.Col5}</td><td>{row.Col6}</td><td>{row.Col7}</td><td>{row.Col8}</td>");
            sb.AppendLine($"<td>{row.Col9}</td><td>{row.Col10}</td><td>{row.Col11}</td><td>{row.Col12}</td>");

            var station = WebUtility.HtmlEncode(row.StationText);
            sb.AppendLine(row.IsSummary
                ? $"<td><strong>{station}</strong></td></tr>"
                : $"<td>{station}</td></tr>");
        }

        sb.AppendLine("</tbody></table>");

        sb.AppendLine("<div class=\"footer\">");
        if (report.IsCumulative)
        {
            sb.AppendLine(
                $"Суммарный отпуск э/э — {report.ReleaseMlnKwh:N3} млн кВт·ч; " +
                $"итого — {report.TotalGkwh:N2} г/кВт·ч · ОПТО");
        }
        else
        {
            sb.AppendLine(
                $"Отпуск э/э за сутки — {report.ReleaseMlnKwh:N3} млн кВт·ч; " +
                $"итого пережогов — {report.TotalGkwh:N2} г/кВт·ч · ОПТО");
        }

        sb.AppendLine("</div>");
        sb.AppendLine("<script>window.addEventListener('load',()=>setTimeout(()=>window.print(),200));</script>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    private static string BuildBlankHtml(DateTime? date)
    {
        var sb = new StringBuilder();
        var dateText = date?.ToString("dd.MM.yyyy") ?? "          ";

        sb.AppendLine("<!DOCTYPE html><html lang=\"ru\"><head><meta charset=\"utf-8\" />");
        sb.AppendLine("<title>ОПТО · Бланк пережогов</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("""
            @page { size: A4 landscape; margin: 8mm 10mm; }
            body { margin:0; padding:10px 12px; font:9px/1.25 "Segoe UI",Arial,sans-serif; color:#111; }
            table { width:100%; border-collapse:collapse; table-layout:fixed; }
            th, td { border:1px solid #bbb; padding:4px 3px; text-align:center; height:22px; }
            th:first-child, td:first-child { text-align:center; width:24px; }
            th:nth-child(2), td:nth-child(2) { text-align:left; width:120px; }
            .title { text-align:center; font-weight:700; margin:8px 0; }
            .subtitle { text-align:center; margin:0 0 12px; color:#444; }
            """);
        sb.AppendLine(OptoPrintChrome.SharedStyles);
        sb.AppendLine("</style></head><body>");
        OptoPrintChrome.AppendToolbar(sb);

        sb.AppendLine($"<div class=\"title\">Бланк исходных данных за {dateText} г.</div>");
        sb.AppendLine("<div class=\"subtitle\">Для расчёта пережогов топлива при наличии типовых отклонений от нормы в структуре тепловой схемы, составе и режиме работающего оборудования блоков</div>");

        sb.AppendLine("<table><thead><tr>");
        sb.AppendLine("<th>N</th><th>Обозначение</th>");
        for (var i = 1; i <= 12; i++)
            sb.AppendLine($"<th>{i}</th>");
        sb.AppendLine("<th>Общие показатели</th></tr></thead><tbody>");

        foreach (var row in PerejegDefinitions.InputRows)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{row.Index}</td>");
            sb.AppendLine($"<td>{WebUtility.HtmlEncode(row.Label)}</td>");
            for (var i = 0; i < 13; i++)
                sb.AppendLine("<td></td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");
        sb.AppendLine("<script>window.addEventListener('load',()=>setTimeout(()=>window.print(),200));</script>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }
}
