using System;
using System.Net;
using System.Text;
using Opto.Models;

namespace Opto.Services;

public static class OptoPrintChrome
{
    public const string SharedStyles = """
        .toolbar { margin-bottom: 10px; }
        .toolbar button {
          border: 0;
          background: #222;
          color: #fff;
          padding: 6px 12px;
          font: 600 12px/1 "Segoe UI", "Helvetica Neue", Arial, sans-serif;
          cursor: pointer;
        }
        .doc-title {
          margin: 0 0 2px;
          font-size: 12px;
          font-weight: 700;
          line-height: 1.3;
          color: #111;
        }
        .doc-meta {
          margin: 0 0 10px;
          font-size: 10px;
          color: #666;
        }
        @media print {
          .toolbar { display: none !important; }
        }
        """;

    public static void AppendToolbar(StringBuilder sb)
    {
        sb.AppendLine("<div class=\"toolbar\">");
        sb.AppendLine("<button type=\"button\" onclick=\"window.print()\">Печать</button>");
        sb.AppendLine("</div>");
    }

    public static void AppendHeader(
        StringBuilder sb,
        string title,
        DateTime date,
        WyrabotkaMode mode,
        string moduleName)
    {
        var modeText = mode == WyrabotkaMode.Calculation ? "Расчёт" : "Перерасчёт";
        sb.AppendLine(
            $"<h1 class=\"doc-title\">{Esc(title)} · {date:dd.MM.yyyy} · {Esc(modeText)}</h1>");
        sb.AppendLine(
            $"<p class=\"doc-meta\">ОПТО · {Esc(moduleName)}</p>");
    }

    private static string Esc(string value) => WebUtility.HtmlEncode(value);
}
