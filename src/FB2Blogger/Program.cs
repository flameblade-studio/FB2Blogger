namespace FB2Blogger;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args is ["--package-smoke-test", var markerPath])
            return RunPackageSmokeTest(markerPath);

        using var singleInstance = new Mutex(true, "Local\\FB2Blogger.SingleInstance", out var isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show(L.T("single_instance"), "FB2Blogger", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 0;
        }
        ApplicationConfiguration.Initialize();
        var settings = SettingsStore.Load();
        L.Configure(settings.InterfaceLanguage);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => CrashReporter.Show(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            CrashReporter.Write(e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()));
        using var mainForm = new MainForm(settings);
        Application.Run(mainForm);
        GC.KeepAlive(singleInstance);
        return 0;
    }

    static int RunPackageSmokeTest(string markerPath)
    {
        try
        {
            if (!Path.IsPathFullyQualified(markerPath))
                throw new ArgumentException("The smoke-test marker path must be fully qualified.", nameof(markerPath));

            ApplicationConfiguration.Initialize();
            L.Configure("en");
            using var mainForm = new MainForm(new AppSettings(), enableInteractiveStartup: false);
            mainForm.Show();
            Application.DoEvents();
            if (!mainForm.Visible || !mainForm.IsHandleCreated)
                throw new InvalidOperationException("The main window did not create a visible native handle.");
            mainForm.Close();
            Application.DoEvents();

            var markerDirectory = Path.GetDirectoryName(markerPath)
                ?? throw new InvalidOperationException("The smoke-test marker directory could not be resolved.");
            Directory.CreateDirectory(markerDirectory);
            File.WriteAllText(
                markerPath,
                $"FB2BLOGGER_WINDOWS_PACKAGE_SMOKE_OK os={Environment.OSVersion} arch={System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
            return 0;
        }
        catch (Exception error)
        {
            try
            {
                if (Path.IsPathFullyQualified(markerPath))
                    File.WriteAllText(markerPath, $"FB2BLOGGER_WINDOWS_PACKAGE_SMOKE_FAILED {error.GetType().Name}: {error.Message}");
            }
            catch
            {
                // The exit code remains the authority when the diagnostic marker cannot be written.
            }
            return 1;
        }
    }
}

internal static class CrashReporter
{
    internal static void Show(Exception error)
    {
        var path = Write(error);
        MessageBox.Show(L.T("unexpected_error", path, error.Message), "FB2Blogger", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    internal static string Write(Exception error)
    {
        try
        {
            var folder = AppPaths.Current.ReportsDirectory;
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, L.T("error_file", DateTime.Now.ToString("yyyyMMdd-HHmmss")));
            File.WriteAllText(path, error.ToString());
            return path;
        }
        catch { return L.T("error_log_unavailable"); }
    }
}
