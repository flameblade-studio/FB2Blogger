using Avalonia;
using System.Runtime.InteropServices;

namespace FB2Blogger.Desktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args is ["--package-smoke-test"])
            return RunPackageSmokeTest();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

    private static int RunPackageSmokeTest()
    {
        try
        {
            _ = BuildAvaloniaApp();
            Console.WriteLine(
                $"FB2BLOGGER_PREVIEW_SMOKE_OK os={RuntimeInformation.OSDescription} arch={RuntimeInformation.ProcessArchitecture}");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"FB2BLOGGER_PREVIEW_SMOKE_FAILED {error.GetType().Name}: {error.Message}");
            return 1;
        }
    }
}
