using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Opto.Models;

namespace Opto.Services;

public static class O2GolPrintService
{
    public static void Print(O2GolReport report)
    {
        var html = BuildHtml(report);
        var path = Path.Combine(
            Path.GetTempPath(),
            $"opto-o2gol-{report.From:yyyyMMdd}-{report.To:yyyyMMdd}-{DateTime.Now:HHmmss}.html");

        File.WriteAllText(path, html, Encoding.UTF8);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        });
    }

    private static string BuildHtml(O2GolReport report)
    {
        var sb = new StringBuilder();
        var period = $"с {report.From:dd.MM} по {report.To:dd.MM}.{report.To:yyyy} г.";

        sb.AppendLine("<!DOCTYPE html><html lang=\"ru\"><head><meta charset=\"utf-8\" />");
        sb.AppendLine($"<title>ОПТО · Сводный расчёт · {report.From:dd.MM.yyyy}–{report.To:dd.MM.yyyy}</title>");
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
            .operator td { font-size:8px; }
            .page-break { page-break-before: always; margin-top: 16px; }
            """);
        sb.AppendLine("</style></head><body>");
        OptoPrintChrome.AppendToolbar(sb);

        sb.AppendLine($"<h1 class=\"doc-title\">Сводный расчёт по вахтам · {Esc(period)}</h1>");
        sb.AppendLine("<p class=\"doc-meta\">ОПТО · Еженедельный сводный расчёт</p>");

        AppendSummarySection(sb, report.ByBlocks);
        AppendSummarySection(sb, report.ByWatches);

        foreach (var group in report.OperatorGroups)
            AppendOperatorGroup(sb, group, period);

        sb.AppendLine("<div class=\"footer\">ОПЕРАТОР ЭВМ</div>");
        sb.AppendLine("<script>window.addEventListener('load',()=>setTimeout(()=>window.print(),200));</script>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static void AppendSummarySection(StringBuilder sb, O2GolSummarySection section)
    {
        sb.AppendLine($"<table><caption class=\"section\">{Esc(section.Title)}</caption>");
        sb.AppendLine("<thead><tr><th></th>");
        foreach (var header in section.ColumnHeaders)
            sb.AppendLine($"<th>{Esc(header)}</th>");
        sb.AppendLine("<th>ПО СТАНЦИИ</th></tr></thead><tbody>");

        foreach (var row in section.Rows)
        {
            sb.AppendLine($"<tr><td>{Esc(row.Label)}</td>");
            foreach (var value in row.Values)
                sb.AppendLine($"<td>{Esc(value)}</td>");
            sb.AppendLine($"<td><strong>{Esc(row.TotalText)}</strong></td></tr>");
        }

        sb.AppendLine("</tbody></table>");
    }

    private static void AppendOperatorGroup(StringBuilder sb, O2GolOperatorGroup group, string period)
    {
        if (group.Rows.Count == 0)
            return;

        sb.AppendLine("<div class=\"page-break\"></div>");
        sb.AppendLine($"<h2 class=\"doc-title\">Результаты расчёта пережога / экономии топлива по машинистам · {Esc(group.Title)}</h2>");
        sb.AppendLine($"<p class=\"doc-meta\">За период {Esc(period)} · ИВЦ ТОШДИЭС</p>");

        sb.AppendLine("<table class=\"operator\"><thead><tr>");
        sb.AppendLine("<th>Маш-ст</th><th>Блок</th><th>Смен</th><th>МВт·ч</th><th>МВт</th>");
        sb.AppendLine("<th>ПУГ</th><th>ВАК</th><th>ДОП</th><th>ТОП</th><th>ТПП</th><th>СН</th><th>ТПВ</th><th>ВСЕГО</th><th>г/кВт·ч</th>");
        sb.AppendLine("</tr></thead><tbody>");

        foreach (var row in group.Rows)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{row.OperatorTn}</td>");
            sb.AppendLine($"<td>{row.BlockNumber}</td>");
            sb.AppendLine($"<td>{row.ShiftCount}</td>");
            sb.AppendLine($"<td>{Esc(row.GenerationText)}</td>");
            sb.AppendLine($"<td>{Esc(row.LoadText)}</td>");
            sb.AppendLine($"<td>{Esc(row.PugText)}</td>");
            sb.AppendLine($"<td>{Esc(row.WakText)}</td>");
            sb.AppendLine($"<td>{Esc(row.DopText)}</td>");
            sb.AppendLine($"<td>{Esc(row.TopText)}</td>");
            sb.AppendLine($"<td>{Esc(row.TppText)}</td>");
            sb.AppendLine($"<td>{Esc(row.SnText)}</td>");
            sb.AppendLine($"<td>{Esc(row.TpwText)}</td>");
            sb.AppendLine($"<td><strong>{Esc(row.TotalText)}</strong></td>");
            sb.AppendLine($"<td>{Esc(row.GkwhText)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");
    }

    private static string Esc(string value) => WebUtility.HtmlEncode(value);
}
