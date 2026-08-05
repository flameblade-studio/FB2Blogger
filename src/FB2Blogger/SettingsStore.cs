using System.Security.Cryptography;
using System.Text.Json;

namespace FB2Blogger;

internal static class SettingsStore
{
    static readonly string Folder = AppPaths.Current.DataDirectory;
    static readonly string FileName = Path.Combine(Folder, "settings.dat");
    static readonly MigrationStateStore MigrationStore = new(Folder);

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FileName)) return new();
            var protectedBytes = File.ReadAllBytes(FileName);
            var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize<AppSettings>(bytes) ?? new();
        }
        catch { return new(); }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Folder);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(settings);
        var temp = FileName + ".tmp";
        File.WriteAllBytes(temp, ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser));
        File.Move(temp, FileName, true);
    }

    public static string StateFile(string zipPath)
        => MigrationStore.LegacyStateFile(zipPath);

    public static string DetailedStateFile(string zipPath) => MigrationStore.DetailedStateFile(zipPath);

    public static MigrationState LoadMigration(string zipPath) => MigrationStore.Load(zipPath);

    public static void SaveMigration(string zipPath, MigrationState state) => MigrationStore.Save(zipPath, state);
}
