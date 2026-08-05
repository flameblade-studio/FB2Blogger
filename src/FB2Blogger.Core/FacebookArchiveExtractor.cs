using System.IO.Compression;

namespace FB2Blogger;

public static class FacebookArchiveExtractor
{
    const int MaximumEntries = 250_000;
    const long MaximumExpandedBytes = 10L * 1024 * 1024 * 1024;
    const long MaximumCompressionRatio = 500;
    const long FreeSpaceReserveBytes = 2L * 1024 * 1024 * 1024;

    public static void Extract(string archive, string target, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archive);
        ArgumentException.ThrowIfNullOrWhiteSpace(target);

        var targetRoot = Path.GetFullPath(target);
        var fullTargetDirectory = Path.GetFullPath(target + Path.DirectorySeparatorChar);
        using var zip = ZipFile.OpenRead(archive);
        if (zip.Entries.Count > MaximumEntries)
            throw new InvalidDataException(L.T("zip_too_many_entries"));

        long totalBytes = 0;
        foreach (var entry in zip.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry.FullName.Length > 1024)
                throw new InvalidDataException(L.T("zip_file_name_too_long"));
            if (IsSymbolicLink(entry))
                throw new InvalidDataException(L.T("zip_unsafe_link"));
            try { totalBytes = checked(totalBytes + entry.Length); }
            catch (OverflowException) { throw new InvalidDataException(L.T("zip_size_invalid")); }
        }

        var archiveBytes = Math.Max(1, new FileInfo(archive).Length);
        if (totalBytes > MaximumExpandedBytes && totalBytes / archiveBytes > MaximumCompressionRatio)
            throw new InvalidDataException(L.T("zip_ratio_invalid"));

        var available = AvailableSpaceForPath(targetRoot);
        var safeAvailable = Math.Max(0, available - FreeSpaceReserveBytes);
        if (totalBytes > safeAvailable)
            throw new IOException(L.T("temp_volume_space_insufficient", FormatBytes(totalBytes), FormatBytes(safeAvailable)));

        Directory.CreateDirectory(targetRoot);
        foreach (var entry in zip.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var destination = Path.GetFullPath(Path.Combine(targetRoot, entry.FullName));
            if (!destination.StartsWith(fullTargetDirectory))
                throw new InvalidDataException(L.T("zip_unsafe_path"));
            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(destination);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            entry.ExtractToFile(destination, true);
        }
    }

    static bool IsSymbolicLink(ZipArchiveEntry entry)
    {
        const int unixFileTypeMask = 0xF000;
        const int unixSymbolicLink = 0xA000;
        var unixMode = (entry.ExternalAttributes >> 16) & 0xFFFF;
        return (unixMode & unixFileTypeMask) == unixSymbolicLink;
    }

    static long AvailableSpaceForPath(string path)
    {
        var readyVolumes = DriveInfo.GetDrives().Where(volume => volume.IsReady).ToArray();
        var selectedRoot = SelectContainingVolumeRoot(
            path,
            readyVolumes.Select(volume => volume.RootDirectory.FullName));
        if (selectedRoot is not null)
        {
            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            var selected = readyVolumes.First(volume =>
                string.Equals(
                    Path.GetFullPath(volume.RootDirectory.FullName).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    Path.GetFullPath(selectedRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    comparison));
            return selected.AvailableFreeSpace;
        }

        var fallbackRoot = Path.GetPathRoot(path) ?? path;
        return new DriveInfo(fallbackRoot).AvailableFreeSpace;
    }

    /// <summary>
    /// Selects the longest mounted-volume root containing a path. This matters on
    /// Unix when the operating-system temp directory is mounted separately from '/'.
    /// </summary>
    public static string? SelectContainingVolumeRoot(string path, IEnumerable<string> volumeRoots)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(volumeRoots);
        var fullPath = Path.GetFullPath(path);

        return volumeRoots
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(Path.GetFullPath)
            .Where(root => IsInsideOrEqual(root, fullPath))
            .OrderByDescending(root => root.Length)
            .FirstOrDefault();
    }

    static bool IsInsideOrEqual(string root, string candidate)
    {
        var relative = Path.GetRelativePath(root, candidate);
        return relative == "." ||
               (!Path.IsPathRooted(relative) &&
                relative != ".." &&
                !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
                !relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal));
    }

    static string FormatBytes(long bytes) => bytes >= 1024L * 1024 * 1024
        ? $"{bytes / (1024d * 1024 * 1024):0.0} GB"
        : $"{bytes / (1024d * 1024):0.0} MB";
}
