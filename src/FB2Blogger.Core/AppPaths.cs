namespace FB2Blogger;

/// <summary>
/// Platform-appropriate application locations. On Windows, DataDirectory is
/// intentionally identical to the legacy LocalAppData\FB2Blogger location.
/// </summary>
public sealed record AppPathSet(string DataDirectory, string ReportsDirectory, string TemporaryRoot);

public static class AppPaths
{
    public static AppPathSet Current { get; } = CreateForCurrentPlatform();

    public static AppPathSet CreateForCurrentPlatform()
    {
        var localData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);
        if (string.IsNullOrWhiteSpace(localData))
        {
            var home = Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile,
                Environment.SpecialFolderOption.DoNotVerify);
            if (string.IsNullOrWhiteSpace(home))
                throw new InvalidOperationException("A writable per-user data directory is unavailable.");
            localData = OperatingSystem.IsMacOS()
                ? Path.Combine(home, "Library", "Application Support")
                : Path.Combine(home, ".local", "share");
        }

        var documents = Environment.GetFolderPath(
            Environment.SpecialFolder.MyDocuments,
            Environment.SpecialFolderOption.DoNotVerify);
        if (string.IsNullOrWhiteSpace(documents)) documents = localData;

        return new(
            Path.Combine(localData, "FB2Blogger"),
            Path.Combine(documents, "FB2Blogger Reports"),
            Path.Combine(Path.GetTempPath(), "FB2Blogger"));
    }
}
