namespace FB2Blogger;

/// <summary>
/// Removes only temporary extraction directories created by the legacy
/// WinForms migration flow. Preview inspection workspaces have their own
/// marker/lease protocol and must be left to <see cref="ArchiveInspectionService"/>.
/// </summary>
public static class LegacyMigrationWorkspaceCleaner
{
    public static void CleanupStaleWorkspaces(AppPathSet paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var root = paths.TemporaryRoot;
        if (!Directory.Exists(root)) return;

        foreach (var directory in Directory.EnumerateDirectories(
                     root,
                     "*",
                     SearchOption.TopDirectoryOnly).ToArray())
        {
            if (!IsOwnedLegacyWorkspace(directory)) continue;
            try { Directory.Delete(directory, recursive: true); }
            catch (DirectoryNotFoundException) { }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    static bool IsOwnedLegacyWorkspace(string directory)
    {
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(directory));

        // Avalonia inspection workspaces are governed by an exclusive lease.
        // The legacy cleaner must never bypass that cross-process ownership.
        if (name.StartsWith(InspectionWorkspaceManager.Prefix, StringComparison.Ordinal)) return false;

        try
        {
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) return false;
            if (File.Exists(Path.Combine(directory, InspectionWorkspaceManager.MarkerFileName))) return false;
            if (File.Exists(directory + InspectionWorkspaceManager.LeaseSuffix)) return false;
        }
        catch (Exception error) when (
            error is IOException or UnauthorizedAccessException or FileNotFoundException or DirectoryNotFoundException)
        {
            // If ownership cannot be proven, preserve the directory.
            return false;
        }

        return Guid.TryParseExact(name, "N", out var id) &&
               string.Equals(name, id.ToString("N"), StringComparison.Ordinal);
    }
}
