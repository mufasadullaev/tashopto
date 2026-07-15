using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Opto.Models;

namespace Opto.Services;

public static class BaxtaPrintService
{
    public static void Print(BaxtaReport report)
    {
        var html = BuildHtml(report);
        var path = Path.Combine(
            Path.GetTempPath(),
            $"opto-baxta-{report.Date:yyyyMMdd}-{DateTime.Now:HHmmss}.html");

        File.WriteAllText(path, html, Encoding.UTF8);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        });
    }

    private static string BuildHtml(BaxtaReport report)
    {
        var mode = report.Mode == WyrabotkaMode.Calculation ? "Расчёт" : "Перерасчёт";
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html><html lang=\"ru\"><head><meta charset=\"utf-8\" />");
        sb.AppendLine($"<title>ОПТО · Вахты · {report.Date:dd.MM.yyyy}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("""
            @page { size: A4 landscape; margin: 8mm 10mm; }
            body { margin:0; padding:10px 12px; font:9px/1.25 "Segoe UI",Arial,sans-serif; color:#111; }
            .toolbar { margin-bottom:10px; }
            .toolbar button { padding:6px 12px; background:#222; color:#fff; border:0; cursor:pointer; }
            h1 { margin:0 0 4px; font-size:12px; }
            .meta { color:#444; margin-bottom:8px; }
            table { width:100%; border-collapse:collapse; margin-bottom:14px; table-layout:fixed; }
            th, td { border-bottom:1px solid #ddd; padding:2px 3px; text-align:right; }
            th:first-child, td:first-child { text-align:left; }
            .section { font-weight:700; background:#f3f6f8; }
            .footer { margin-top:8px; font-size:10px; }
            @media print { .toolbar { display:none; } }
            """);
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<div class=\"toolbar\"><button onclick=\"window.print()\">Печать</button></div>");
        sb.AppendLine($"<h1>ИТОГИ РАСХОДА ТОПЛИВА (в кг у.т.) по сменам, вахтам и блокам · {report.Date:dd.MM.yyyy} · {mode}</h1>");
        sb.AppendLine("<p class=\"meta\">ОПТО · Ежедневный расчёт по вахтам</p>");

        foreach (var section in report.Sections)
        {
            var watch = section.Watch is not null ? $" · вахта {section.Watch}" : "";
            sb.AppendLine($"<table><caption class=\"section\">{WebUtility.HtmlEncode(section.Title)}{watch}</caption>");
            sb.AppendLine("<thead><tr><th></th>");
            for (var i = 1; i <= 12; i++)
                sb.AppendLine($"<th>{i}</th>");
            sb.AppendLine("<th>ПО СТАНЦИИ</th></tr></thead><tbody>");

        foreach (var row in section.Rows)
        {
            sb.AppendLine($"<tr><td>{WebUtility.HtmlEncode(row.Label)}</td>");
            sb.AppendLine($"<td>{row.Col1}</td><td>{row.Col2}</td><td>{row.Col3}</td><td>{row.Col4}</td>");
            sb.AppendLine($"<td>{row.Col5}</td><td>{row.Col6}</td><td>{row.Col7}</td><td>{row.Col8}</td>");
            sb.AppendLine($"<td>{row.Col9}</td><td>{row.Col10}</td><td>{row.Col11}</td><td>{row.Col12}</td>");
            sb.AppendLine($"<td><strong>{row.TotalText}</strong></td></tr>");
        }

            sb.AppendLine("</tbody></table>");
        }

        sb.AppendLine("<div class=\"footer\">");
        sb.AppendLine($"Справка по удельному расходу топлива: план на МЕСЯЦ — {report.Urp:N2} г/кВт·ч; ");
        sb.AppendLine($"факт с начала МЕСЯЦА — {report.Urm:N2} г/кВт·ч · ОПЕРАТОР НТЦ");
        sb.AppendLine("</div>");
        sb.AppendLine("<script>window.addEventListener('load',()=>setTimeout(()=>window.print(),200));</script>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}
