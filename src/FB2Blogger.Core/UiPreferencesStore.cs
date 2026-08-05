using System.Text.Json;

namespace FB2Blogger;

public sealed class UiPreferencesStore(AppPathSet paths)
{
    readonly string path = Path.Combine(paths.DataDirectory, "ui-preferences.json");

    public string LoadLanguage()
    {
        try
        {
            if (!File.Exists(path)) return "";
            var model = JsonSerializer.Deserialize<UiPreferences>(File.ReadAllText(path));
            return L.SupportedCodes.Contains(model?.InterfaceLanguage ?? "", StringComparer.Ordinal)
                ? model!.InterfaceLanguage
                : "";
        }
        catch { return ""; }
    }

    public void SaveLanguage(string language)
    {
        if (!L.SupportedCodes.Contains(language, StringComparer.Ordinal))
            throw new ArgumentOutOfRangeException(nameof(language));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(new UiPreferences(language)));
        File.Move(temporary, path, true);
    }

    sealed record UiPreferences(string InterfaceLanguage);
}
