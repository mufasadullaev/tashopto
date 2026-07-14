using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Opto.Views;

public class NumericTextBox : TextBox
{
    public static readonly StyledProperty<string> InputModeProperty =
        AvaloniaProperty.Register<NumericTextBox, string>(nameof(InputMode), "decimal");

    private bool _sanitizing;

    public string InputMode
    {
        get => GetValue(InputModeProperty);
        set => SetValue(InputModeProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(TextBox);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty && !_sanitizing)
            Sanitize();
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        var incoming = (e.Text ?? "").Replace(',', '.');
        if (!IsAllowedFragment(incoming))
        {
            e.Handled = true;
            return;
        }

        var current = Text ?? "";
        var start = Math.Clamp(SelectionStart, 0, current.Length);
        var end = Math.Clamp(SelectionEnd, 0, current.Length);
        if (end < start)
            (start, end) = (end, start);

        var next = current.Remove(start, end - start).Insert(start, incoming);
        if (!IsValidValue(next))
        {
            e.Handled = true;
            return;
        }

        if (e.Text != incoming)
        {
            e.Handled = true;
            _sanitizing = true;
            Text = next;
            CaretIndex = start + incoming.Length;
            _sanitizing = false;
            return;
        }

        base.OnTextInput(e);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        NormalizeOnBlur();
    }

    private void Sanitize()
    {
        var raw = Text ?? "";
        var cleaned = Clean(raw);
        if (cleaned == raw)
            return;

        var caret = CaretIndex;
        _sanitizing = true;
        Text = cleaned;
        CaretIndex = Math.Min(caret, cleaned.Length);
        _sanitizing = false;
    }

    private void NormalizeOnBlur()
    {
        var text = Clean(Text ?? "").Trim();
        if (string.IsNullOrEmpty(text) || text == ".")
        {
            SetTextSafe("0");
            return;
        }

        if (IsIntegerMode)
        {
            SetTextSafe(int.TryParse(text, NumberStyles.Integer, OptoCulture.Current, out var i)
                ? i.ToString(OptoCulture.Current)
                : "0");
            return;
        }

        SetTextSafe(decimal.TryParse(
            text,
            NumberStyles.AllowDecimalPoint,
            OptoCulture.Current,
            out var value)
            ? value.ToString("0.######", OptoCulture.Current)
            : "0");
    }

    private void SetTextSafe(string value)
    {
        if (Text == value)
            return;

        _sanitizing = true;
        Text = value;
        _sanitizing = false;
    }

    private bool IsIntegerMode =>
        string.Equals(InputMode, "integer", StringComparison.OrdinalIgnoreCase);

    private bool IsAllowedFragment(string incoming)
    {
        if (incoming.Length == 0)
            return false;

        return IsIntegerMode
            ? incoming.All(char.IsDigit)
            : incoming.All(ch => char.IsDigit(ch) || ch == '.');
    }

    private bool IsValidValue(string value)
    {
        if (IsIntegerMode)
            return value.Length == 0 || value.All(char.IsDigit);

        if (value.Count(c => c == '.') > 1)
            return false;

        return value.All(ch => char.IsDigit(ch) || ch == '.');
    }

    private string Clean(string raw)
    {
        var sb = new StringBuilder(raw.Length);
        var sawDot = false;

        foreach (var ch in raw)
        {
            var c = ch == ',' ? '.' : ch;

            if (IsIntegerMode)
            {
                if (char.IsDigit(c))
                    sb.Append(c);
                continue;
            }

            if (char.IsDigit(c))
            {
                sb.Append(c);
                continue;
            }

            if (c == '.' && !sawDot)
            {
                sb.Append('.');
                sawDot = true;
            }
        }

        return sb.ToString();
    }
}
