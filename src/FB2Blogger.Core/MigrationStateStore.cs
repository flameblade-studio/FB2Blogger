using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FB2Blogger;

public sealed class MigrationStateStore
{
    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    readonly string folder;

    public MigrationStateStore(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        this.folder = Path.GetFullPath(folder);
    }

    public string LegacyStateFile(string zipPath)
    {
        var identity = Path.GetFullPath(zipPath);
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, $"completed-{key}.txt");
    }

    public string DetailedStateFile(string zipPath) => Path.ChangeExtension(LegacyStateFile(zipPath), ".json");

    public MigrationState Load(string zipPath)
    {
        try
        {
            var path = DetailedStateFile(zipPath);
            if (!File.Exists(path)) return new();
            try { return JsonSerializer.Deserialize<MigrationState>(File.ReadAllText(path)) ?? new(); }
            catch (JsonException) when (File.Exists(path + ".bak"))
            {
                return JsonSerializer.Deserialize<MigrationState>(File.ReadAllText(path + ".bak")) ?? new();
            }
        }
        catch { return new(); }
    }

    public void Save(string zipPath, MigrationState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Directory.CreateDirectory(folder);
        var path = DetailedStateFile(zipPath);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(state, JsonOptions));
        if (File.Exists(path)) File.Copy(path, path + ".bak", true);
        File.Move(temporary, path, true);
    }
}
