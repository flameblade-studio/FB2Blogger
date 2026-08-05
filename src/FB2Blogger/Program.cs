namespace FB2Blogger;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        using var singleInstance = new Mutex(true, "Local\\FB2Blogger.SingleInstance", out var isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show("FB2Blogger 已經在執行中，請回到原本的視窗。", "FB2Blogger", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => CrashReporter.Show(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            CrashReporter.Write(e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()));
        using var mainForm = new MainForm();
        Application.Run(mainForm);
        GC.KeepAlive(singleInstance);
    }
}

internal static class CrashReporter
{
    internal static void Show(Exception error)
    {
        var path = Write(error);
        MessageBox.Show($"程式遇到未預期問題，已留下錯誤紀錄：\n{path}\n\n{error.Message}", "FB2Blogger", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    internal static string Write(Exception error)
    {
        try
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FB2Blogger Reports");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, $"錯誤-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            File.WriteAllText(path, error.ToString());
            return path;
        }
        catch { return "無法寫入錯誤紀錄"; }
    }
}
