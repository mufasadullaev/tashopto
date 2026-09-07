using System.Text;
using Opto.Models;

namespace Opto.Services;

public static class AktPrintService
{
    public static string BuildDocument(AktReport report)
    {
        var html = new StringBuilder();
        var dateStr = report.Date.ToString("MMMM yyyy", OptoCulture.Current);

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><meta charset='utf-8'>");
        html.AppendLine("<title>АКТ ВЫРАБОТКИ И ОТПУСКА ЭЛЕКТРОЭНЕРГИИ</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: monospace, sans-serif; font-size: 13px; margin: 20px; }");
        html.AppendLine("h2 { text-align: center; margin-bottom: 5px; }");
        html.AppendLine(".subtitle { text-align: center; margin-bottom: 20px; font-weight: bold; }");
        html.AppendLine("table { width: 100%; border-collapse: collapse; margin-bottom: 15px; }");
        html.AppendLine("th, td { border: 1px solid #000; padding: 4px 8px; text-align: right; }");
        html.AppendLine("th { background-color: #f0f0f0; text-align: center; }");
        html.AppendLine(".label-col { text-align: left; }");
        html.AppendLine(".summary-row { font-weight: bold; background-color: #fafafa; }");
        html.AppendLine("</style></head><body>");

        html.AppendLine($"<h2>АКТ ВЫРАБОТКИ И ОТПУСКА ЭЛЕКТРОЭНЕРГИИ</h2>");
        html.AppendLine($"<div class='subtitle'>ТашТЭС за {dateStr}г.</div>");

        html.AppendLine("<h3>1. СОБСТВЕННЫЕ НУЖДЫ (тыс. кВт·ч)</h3>");
        html.AppendLine("<table><thead><tr><th>Наименование</th><th>Значение</th></tr></thead><tbody>");
        html.AppendLine($"<tr><td class='label-col'>Собственные нужды блоков</td><td>{report.BlockOwnNeedsTotalKwh / 1000m:N1}</td></tr>");
        html.AppendLine($"<tr><td class='label-col'>Трансформаторы СН</td><td>{report.TransformerOwnNeedsTotalKwh / 1000m:N1}</td></tr>");
        html.AppendLine($"<tr><td class='label-col'>Резервный возбудитель</td><td>{report.ReserveExciterKwh / 1000m:N1}</td></tr>");
        html.AppendLine($"<tr><td class='label-col'>Потери</td><td>{report.LossesKwh / 1000m:N1}</td></tr>");
        html.AppendLine($"<tr><td class='label-col'>Хозяйственные нужды</td><td>{report.PlantFacilitiesKwh / 1000m:N1}</td></tr>");
        html.AppendLine($"<tr><td class='label-col'>Профилакторий</td><td>{report.PreventoriumKwh / 1000m:N1}</td></tr>");
        html.AppendLine($"<tr class='summary-row'><td class='label-col'>ИТОГО СОБСТВЕННЫЕ НУЖДЫ</td><td>{report.TotalOwnNeedsKwh / 1000m:N1}</td></tr>");
        html.AppendLine("</tbody></table>");

        html.AppendLine("<h3>2. ВЫРАБОТКА И ОТПУСК (тыс. кВт·ч)</h3>");
        html.AppendLine("<table><thead><tr><th>Показатель</th><th>Значение</th></tr></thead><tbody>");
        html.AppendLine($"<tr><td class='label-col'>ВЫРАБОТКА (брутто)</td><td>{report.GrossGenerationKwh / 1000m:N1}</td></tr>");
        html.AppendLine($"<tr><td class='label-col'>СОБСТВЕННЫЕ НУЖДЫ</td><td>{report.TotalOwnNeedsKwh / 1000m:N1}</td></tr>");
        html.AppendLine($"<tr class='summary-row'><td class='label-col'>ПОЛЕЗНЫЙ ОТПУСК В СЕТЬ</td><td>{report.NetReleaseKwh / 1000m:N1}</td></tr>");
        html.AppendLine("</tbody></table>");

        html.AppendLine("</body></html>");
        return html.ToString();
    }
}
