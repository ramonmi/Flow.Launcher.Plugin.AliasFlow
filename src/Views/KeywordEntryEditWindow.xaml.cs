using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;

using Microsoft.Win32;

using Flow.Launcher.Plugin.AliasFlow.Models;

namespace Flow.Launcher.Plugin.AliasFlow.Views;

public partial class KeywordEntryEditWindow : Window
{
    public KeywordEntry Result { get; private set; } = new();

    public KeywordEntryEditWindow(KeywordEntry? seed)
    {
        InitializeComponent();
        ApplyTheme();
        SourceInitialized += (_, __) => ApplyTheme();
        Loaded += (_, __) => ApplyTheme();

        if (seed != null)
        {
            TitleBox.Text = seed.Title ?? "";
            DescBox.Text = seed.Description ?? "";
            PathBox.Text = seed.Path ?? "";
            HotkeyBox.Text = seed.Hotkey ?? "";
            KeywordsBox.Text = seed.Keywords is null ? "" : string.Join(", ", seed.Keywords);
        }
    }

    private void ApplyTheme()
    {
        var isDark = IsDarkTheme();
        ApplyTitleBarTheme(isDark);

        if (isDark)
        {
            ApplyDarkThemeResources();
            return;
        }

        ClearValue(BackgroundProperty);
        ClearValue(ForegroundProperty);
        Resources.Remove(typeof(TextBlock));
        Resources.Remove(typeof(TextBox));
    }

    private void ApplyDarkThemeResources()
    {
        var windowBg = CreateBrush("#FF1E1E1E");
        var textFg = CreateBrush("#FFF1F1F1");
        var inputBg = CreateBrush("#FF2B2B2B");
        var inputBorder = CreateBrush("#FF505050");

        Background = windowBg;
        Foreground = textFg;

        var textBlockBaseStyle = TryFindResource(typeof(TextBlock)) as Style;
        var textBlockStyle = new Style(typeof(TextBlock), textBlockBaseStyle);
        textBlockStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, textFg));
        Resources[typeof(TextBlock)] = textBlockStyle;

        var textBoxBaseStyle = TryFindResource(typeof(TextBox)) as Style;
        var textBoxStyle = new Style(typeof(TextBox), textBoxBaseStyle);
        textBoxStyle.Setters.Add(new Setter(TextBox.BackgroundProperty, inputBg));
        textBoxStyle.Setters.Add(new Setter(TextBox.ForegroundProperty, textFg));
        textBoxStyle.Setters.Add(new Setter(TextBox.BorderBrushProperty, inputBorder));
        textBoxStyle.Setters.Add(new Setter(TextBox.CaretBrushProperty, textFg));
        textBoxStyle.Setters.Add(new Setter(TextBox.BorderThicknessProperty, new Thickness(1)));
        textBoxStyle.Setters.Add(new Setter(TextBox.PaddingProperty, new Thickness(8, 4, 8, 4)));
        Resources[typeof(TextBox)] = textBoxStyle;
    }

    private void ApplyTitleBarTheme(bool isDark)
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            var value = isDark ? 1 : 0;

            // Windows 11 / newer builds
            _ = DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int));
            // Windows 10 fallback
            _ = DwmSetWindowAttribute(hwnd, 19, ref value, sizeof(int));
        }
        catch
        {
            // ignore - keep default title bar behavior on unsupported systems
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

    private static bool IsDarkTheme()
    {
        try
        {
            using var personalize = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false);
            var raw = personalize?.GetValue("AppsUseLightTheme");
            if (raw is int light)
                return light == 0;
        }
        catch
        {
            // ignored
        }

        return false;
    }

    private static SolidColorBrush CreateBrush(string colorHex)
    {
        var color = (Color)ColorConverter.ConvertFromString(colorHex);
        return new SolidColorBrush(color);
    }

    private void HotkeyClearButton_Click(object sender, RoutedEventArgs e)
    {
        HotkeyBox.Text = "";
        HotkeyBox.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var title = (TitleBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show("Title is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var keywords = (KeywordsBox.Text ?? "")
            .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Result = new KeywordEntry
        {
            Title = title,
            Description = (DescBox.Text ?? "").Trim(),
            Path = (PathBox.Text ?? "").Trim(),
            Hotkey = (HotkeyBox.Text ?? "").Trim(),
            Keywords = keywords
        };

        DialogResult = true;
        Close();
    }
}
