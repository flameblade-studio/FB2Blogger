using System.Text;

namespace FB2Blogger;

internal sealed class InspectionWorkspaceManager(
    AppPathSet paths,
    ITemporaryDirectoryCleaner directoryCleaner)
{
    internal const string MarkerFileName = ".fb2blogger-inspection-workspace";
    internal const string MarkerContents = "FB2Blogger inspection workspace v1";
    internal const string LeaseSuffix = ".lease";
    const string LeaseHeader = "FB2Blogger inspection lease v1";
    internal const string Prefix = "inspect-";

    internal InspectionWorkspaceLease CreateWorkspace()
    {
        Directory.CreateDirectory(paths.TemporaryRoot);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var id = Guid.NewGuid();
            var name = WorkspaceName(id);
            var directory = Path.Combine(paths.TemporaryRoot, name);
            var leasePath = Path.Combine(paths.TemporaryRoot, name + LeaseSuffix);
            FileStream? lease = null;
            try
            {
                // The trusted, exclusively-held lease is durable before the
                // directory becomes visible. This closes the creation race with
                // cleanup running in another process.
                lease = new FileStream(leasePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
                WriteTrustedLease(lease, id);
                Directory.CreateDirectory(directory);
                File.WriteAllText(
                    Path.Combine(directory, MarkerFileName),
                    MarkerContents,
                    new UTF8Encoding(false));
                return new(directory, leasePath, lease);
            }
            catch (IOException) when (lease is null && File.Exists(leasePath))
            {
                // A practically impossible GUID collision; generate another name.
            }
            catch (Exception creationFailure)
            {
                lease?.Dispose();
                var cleanupFailure = TryDeleteIncompleteWorkspace(directory, leasePath);
                if (cleanupFailure is not null)
                    throw new AggregateException(
                        L.T("temp_operation_and_cleanup_failed"),
                        creationFailure,
                        cleanupFailure);
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(creationFailure).Throw();
                throw new InvalidOperationException("Unreachable code.");
            }
        }

        throw new IOException("Could not allocate a unique FB2Blogger inspection workspace.");
    }

    internal void CleanupStaleWorkspaces(Action<string>? log = null)
    {
        if (!Directory.Exists(paths.TemporaryRoot)) return;

        var failures = new List<Exception>();
        var namesWithLease = new HashSet<string>(StringComparer.Ordinal);
        foreach (var leasePath in Directory.EnumerateFiles(
                     paths.TemporaryRoot,
                     "*",
                     SearchOption.TopDirectoryOnly).ToArray())
        {
            if (!TryParseLeasePath(leasePath, out var name, out var id)) continue;
            namesWithLease.Add(name);
            CleanupLeaseCandidate(leasePath, name, id, failures);
        }

        // Compatibility path for a program-owned marked workspace whose lease
        // was already removed. Any strict-name directory with an untrusted,
        // locked, or active lease is deliberately excluded here.
        foreach (var directory in Directory.EnumerateDirectories(
                     paths.TemporaryRoot,
                     "*",
                     SearchOption.TopDirectoryOnly).ToArray())
        {
            if (!TryGetOwnedWorkspaceName(directory, out var name) || namesWithLease.Contains(name)) continue;
            TryDeleteDirectory(directory, failures);
        }

        if (failures.Count == 0) return;
        var message = L.T("temp_cleanup_retry_failed", failures.Count, paths.TemporaryRoot);
        log?.Invoke(message);
        throw new IOException(
            message,
            failures.Count == 1 ? failures[0] : new AggregateException(failures));
    }

    internal void ReleaseAndDelete(InspectionWorkspaceLease workspace)
    {
        workspace.Dispose();
        try
        {
            directoryCleaner.DeleteRecursively(workspace.DirectoryPath);
        }
        catch (DirectoryNotFoundException)
        {
            // A concurrent recovery pass completed the cleanup first.
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // Keep the now-unlocked trusted lease so the next launch can retry.
            throw new IOException(L.T("temp_cleanup_failed", workspace.DirectoryPath), error);
        }

        try
        {
            File.Delete(workspace.LeasePath);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // A crash here is recoverable because orphan trusted leases are
            // enumerated independently at the next startup.
            throw new IOException(L.T("temp_lease_cleanup_failed", workspace.LeasePath), error);
        }
    }

    internal bool TryGetOwnedWorkspaceName(string directory, out string name)
    {
        name = Path.GetFileName(Path.TrimEndingDirectorySeparator(directory));
        if (!TryParseWorkspaceName(name, out _) || !IsPlainDirectory(directory)) return false;

        var marker = Path.Combine(directory, MarkerFileName);
        try
        {
            return File.Exists(marker) &&
                   string.Equals(File.ReadAllText(marker).Trim(), MarkerContents, StringComparison.Ordinal);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    internal static string LeaseContents(Guid id) => $"{LeaseHeader}\n{id:N}\n";
    internal static string WorkspaceName(Guid id) => Prefix + id.ToString("N");

    void CleanupLeaseCandidate(
        string leasePath,
        string name,
        Guid id,
        List<Exception> failures)
    {
        FileStream? lease = null;
        var trusted = false;
        try
        {
            // The path came from an enumeration snapshot. It may disappear
            // before attributes are read, so the reparse-point check belongs
            // to the same race-safe boundary as opening the lease.
            if (IsReparsePoint(leasePath)) return;

            try
            {
                lease = new FileStream(leasePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException)
            {
                // A concurrent cleanup removed the enumerated lease first.
                return;
            }
            catch (IOException)
            {
                // An active process holds the lease, or exclusive ownership
                // cannot be proven. In either case, do not touch it.
                return;
            }

            trusted = HasTrustedLeaseContents(lease, id);
            if (!trusted) return;

            var directory = Path.Combine(paths.TemporaryRoot, name);
            if (Directory.Exists(directory))
            {
                try
                {
                    if (IsReparsePoint(directory)) return;
                }
                catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException)
                {
                    // The directory vanished after the existence check. Keep
                    // going so the now-orphaned trusted lease is removed too.
                }
                TryDeleteDirectory(directory, failures);
            }
        }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException)
        {
            // Another process completed the cleanup after enumeration.
            return;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            failures.Add(error);
        }
        finally
        {
            lease?.Dispose();
        }

        var workspacePath = Path.Combine(paths.TemporaryRoot, name);
        if (trusted && !Directory.Exists(workspacePath)) TryDeleteLeaseFile(leasePath, failures);
    }

    void TryDeleteDirectory(string directory, List<Exception> failures)
    {
        try { directoryCleaner.DeleteRecursively(directory); }
        catch (DirectoryNotFoundException) { }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { failures.Add(error); }
    }

    static bool HasTrustedLeaseContents(FileStream lease, Guid id)
    {
        var expected = Encoding.UTF8.GetBytes(LeaseContents(id));
        try
        {
            // Reject malformed and oversized files before allocating or
            // decoding anything. The trusted payload is fixed-size ASCII.
            if (lease.Length != expected.Length) return false;

            lease.Position = 0;
            Span<byte> actual = stackalloc byte[expected.Length];
            var offset = 0;
            while (offset < actual.Length)
            {
                var count = lease.Read(actual[offset..]);
                if (count == 0) return false;
                offset += count;
            }
            return actual.SequenceEqual(expected);
        }
        catch (Exception error) when (
            error is IOException or UnauthorizedAccessException or NotSupportedException or ObjectDisposedException)
        {
            return false;
        }
    }

    static void WriteTrustedLease(FileStream lease, Guid id)
    {
        var contents = Encoding.UTF8.GetBytes(LeaseContents(id));
        lease.Write(contents);
        lease.Flush(flushToDisk: true);
        lease.Position = 0;
    }

    static bool TryParseLeasePath(string leasePath, out string workspaceName, out Guid id)
    {
        var fileName = Path.GetFileName(leasePath);
        workspaceName = "";
        id = default;
        if (!fileName.EndsWith(LeaseSuffix, StringComparison.Ordinal)) return false;
        workspaceName = fileName[..^LeaseSuffix.Length];
        return TryParseWorkspaceName(workspaceName, out id);
    }

    static bool TryParseWorkspaceName(string name, out Guid id)
    {
        id = default;
        return name.StartsWith(Prefix, StringComparison.Ordinal) &&
               name.Length == Prefix.Length + 32 &&
               Guid.TryParseExact(name[Prefix.Length..], "N", out id) &&
               name == WorkspaceName(id);
    }

    static bool IsPlainDirectory(string directory)
    {
        try { return !IsReparsePoint(directory); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return false; }
    }

    static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    static void TryDeleteLeaseFile(string leasePath, List<Exception> failures)
    {
        try { File.Delete(leasePath); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { failures.Add(error); }
    }

    static Exception? TryDeleteIncompleteWorkspace(string directory, string leasePath)
    {
        var failures = new List<Exception>();
        try { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { failures.Add(error); }

        // Never discard the recovery identity while its directory still exists.
        if (!Directory.Exists(directory))
        {
            try { File.Delete(leasePath); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { failures.Add(error); }
        }

        return failures.Count switch
        {
            0 => null,
            1 => failures[0],
            _ => new AggregateException(failures)
        };
    }
}

internal sealed class InspectionWorkspaceLease(
    string directoryPath,
    string leasePath,
    FileStream handle) : IDisposable
{
    bool disposed;

    internal string DirectoryPath { get; } = directoryPath;
    internal string LeasePath { get; } = leasePath;

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        handle.Dispose();
    }
}
