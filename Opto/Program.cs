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

        if (Array.Exists(args, a => a.Equals("--seed", StringComparison.OrdinalIgnoreCase) || a.Equals("--seed-3days", StringComparison.OrdinalIgnoreCase)))
        {
            Opto.Services.OptoDataSeeder.ClearAndSeed3Days();
            return;
        }

        if (Array.Exists(args, a => a.Equals("--test", StringComparison.OrdinalIgnoreCase) || a.Equals("--test-all", StringComparison.OrdinalIgnoreCase)))
        {
            Opto.Services.OptoTestRunner.RunAllTests();
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
