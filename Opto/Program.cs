using Avalonia;
using System;
using System.Globalization;
using System.Threading;

namespace Opto;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = OptoCulture.Current;
        CultureInfo.DefaultThreadCurrentUICulture = OptoCulture.Current;
        Thread.CurrentThread.CurrentCulture = OptoCulture.Current;
        Thread.CurrentThread.CurrentUICulture = OptoCulture.Current;

        Opto.Services.Database.OptoDatabase.Initialize();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
