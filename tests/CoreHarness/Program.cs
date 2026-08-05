using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;
using FB2Blogger;

if (args.Length == 2 && args[0] == "--hold-lease")
{
    try
    {
        using var heldLease = new FileStream(args[1], FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Console.WriteLine("READY");
        await Console.Out.FlushAsync();
        await Console.In.ReadLineAsync();
        return 0;
    }
    catch (Exception error)
    {
        Console.Error.WriteLine(error.GetType().Name + ": " + error.Message);
        return 2;
    }
}

var failures = new List<string>();
void Check(bool condition, string name)
{
    Console.WriteLine($"{(condition ? "PASS" : "FAIL")} {name}");
    if (!condition) failures.Add(name);
}

var root = Path.Combine(Path.GetTempPath(), "FB2Blogger-CoreHarness-" + Guid.NewGuid().ToString("N"));
var paths = new AppPathSet(Path.Combine(root, "data"), Path.Combine(root, "reports"), Path.Combine(root, "temp"));
Directory.CreateDirectory(root);
try
{
    foreach (var language in L.SupportedCodes)
    {
        L.Configure(language);
        Check(L.T("desktop_title") != "desktop_title", $"Desktop preview localization resolves in {language}");
    }
    var keys = L.SupportedCodes.Select(L.Keys).ToArray();
    Check(keys.All(candidate => candidate.Order().SequenceEqual(keys[0].Order())), "All four languages expose the same catalog keys");

    var currentPaths = AppPaths.CreateForCurrentPlatform();
    Check(Path.IsPathFullyQualified(currentPaths.DataDirectory), "Per-user data path is fully qualified");
    Check(Path.IsPathFullyQualified(currentPaths.ReportsDirectory), "Report path is fully qualified");
    if (OperatingSystem.IsWindows())
    {
        var legacyWindowsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FB2Blogger");
        Check(currentPaths.DataDirectory == legacyWindowsPath, "Windows keeps the existing LocalAppData path");
    }

    var simulatedFileSystemRoot = Path.GetPathRoot(Path.GetFullPath(root))!;
    var simulatedTempVolume = Path.Combine(simulatedFileSystemRoot, "separate-temp-volume");
    var selectedVolume = FacebookArchiveExtractor.SelectContainingVolumeRoot(
        Path.Combine(simulatedTempVolume, "FB2Blogger", "inspect-workspace"),
        [simulatedFileSystemRoot, simulatedTempVolume]);
    Check(
        selectedVolume == Path.GetFullPath(simulatedTempVolume),
        "Disk-space checks select the actual nested temporary volume");

    L.Configure("zh-TW");
    var export = Path.Combine(root, "export");
    Directory.CreateDirectory(export);
    var legacyText = Encoding.Latin1.GetString(Encoding.UTF8.GetBytes("舊版中文 😀 #舊標籤"));
    var posts = new object[]
    {
        new { timestamp = 1700000002L, data = new[] { new { post = legacyText } } },
        new
        {
            timestamp = 1700000001L,
            data = new[] { new { post = "繁體中文 😀 #標籤" } },
            attachments = new[] { new { data = new[] { new { media = new { uri = "media/photo.jpg" } } } } }
        }
    };
    var postsJson = JsonSerializer.Serialize(posts);
    File.WriteAllText(Path.Combine(export, "your_posts_1.json"), postsJson, new UTF8Encoding(false));
    var parsed = FacebookParser.Read(export, _ => { });
    Check(parsed.Count == 2, "Parser reads Facebook posts without a Windows UI");
    Check(parsed[0].Text == "繁體中文 😀 #標籤", "Modern UTF-8 and emoji are preserved");
    Check(parsed[1].Text == "舊版中文 😀 #舊標籤", "Legacy Facebook mojibake is repaired");

    var uppercaseExport = Path.Combine(root, "uppercase-export");
    Directory.CreateDirectory(uppercaseExport);
    var uppercasePosts = new[]
    {
        new { timestamp = 1700000100L, data = new[] { new { post = "Uppercase JSON extension" } } }
    };
    File.WriteAllText(
        Path.Combine(uppercaseExport, "YOUR_POSTS.JSON"),
        JsonSerializer.Serialize(uppercasePosts),
        new UTF8Encoding(false));
    Check(
        FacebookParser.Read(uppercaseExport, _ => { }).Count == 1,
        "Parser accepts uppercase JSON extensions on case-sensitive filesystems");

    var normalZip = Path.Combine(root, "normal.zip");
    using (var zip = ZipFile.Open(normalZip, ZipArchiveMode.Create))
    {
        var entry = zip.CreateEntry("your_posts_1.json");
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(postsJson);
    }
    var extracted = Path.Combine(root, "extracted");
    FacebookArchiveExtractor.Extract(normalZip, extracted);
    Check(File.Exists(Path.Combine(extracted, "your_posts_1.json")), "Safe archive extraction works");

    var traversalZip = Path.Combine(root, "traversal.zip");
    using (var zip = ZipFile.Open(traversalZip, ZipArchiveMode.Create))
    {
        var entry = zip.CreateEntry("../escape.txt");
        using var writer = new StreamWriter(entry.Open());
        writer.Write("blocked");
    }
    var traversalBlocked = false;
    try { FacebookArchiveExtractor.Extract(traversalZip, Path.Combine(root, "unsafe")); }
    catch (InvalidDataException) { traversalBlocked = true; }
    Check(traversalBlocked && !File.Exists(Path.Combine(root, "escape.txt")), "ZIP path traversal is blocked on every platform");

    var prefixCollisionZip = Path.Combine(root, "prefix-collision.zip");
    using (var zip = ZipFile.Open(prefixCollisionZip, ZipArchiveMode.Create))
    {
        var entry = zip.CreateEntry("../unsafe-sibling/escape.txt");
        using var writer = new StreamWriter(entry.Open());
        writer.Write("blocked");
    }
    var prefixCollisionBlocked = false;
    try { FacebookArchiveExtractor.Extract(prefixCollisionZip, Path.Combine(root, "unsafe")); }
    catch (InvalidDataException) { prefixCollisionBlocked = true; }
    Check(
        prefixCollisionBlocked && !File.Exists(Path.Combine(root, "unsafe-sibling", "escape.txt")),
        "ZIP extraction enforces a directory boundary instead of a string-prefix match");

    var linkZip = Path.Combine(root, "link.zip");
    using (var zip = ZipFile.Open(linkZip, ZipArchiveMode.Create))
    {
        var entry = zip.CreateEntry("link");
        entry.ExternalAttributes = unchecked((int)0xA0000000);
        using var writer = new StreamWriter(entry.Open());
        writer.Write("target");
    }
    var linkBlocked = false;
    try { FacebookArchiveExtractor.Extract(linkZip, Path.Combine(root, "link-out")); }
    catch (InvalidDataException) { linkBlocked = true; }
    Check(linkBlocked, "Archive symbolic links are rejected before extraction");

    var stateStore = new MigrationStateStore(paths.DataDirectory);
    var identityZip = Path.Combine(root, "identity.zip");
    File.WriteAllText(identityZip, "identity");
    var firstState = new MigrationState();
    firstState.Posts["first"] = new PostState { Complete = true };
    stateStore.Save(identityZip, firstState);
    var secondState = new MigrationState();
    secondState.Posts["second"] = new PostState { Complete = true };
    stateStore.Save(identityZip, secondState);
    File.WriteAllText(stateStore.DetailedStateFile(identityZip), "corrupt");
    Check(stateStore.Load(identityZip).Posts.ContainsKey("first"), "Corrupt state recovers from the atomic backup");

    var preferences = new UiPreferencesStore(paths);
    preferences.SaveLanguage("ja");
    Check(preferences.LoadLanguage() == "ja", "Non-secret interface preference uses the per-user data folder");

    var sample = new FacebookPost("key<&", "Title", "Body <tag>", DateTimeOffset.Now, [], []);
    var html = MigrationContent.BeginPostHtml(sample);
    Check(html.Contains("key&lt;&amp;", StringComparison.Ordinal) && html.Contains("Body &lt;tag&gt;", StringComparison.Ordinal), "Blogger HTML encodes imported content");
    Check(MigrationContent.IsVideoPath("clip.MP4") && !MigrationContent.IsVideoPath("photo.jpg"), "Media classification is platform-neutral");

    var inspection = await new ArchiveInspectionService(paths).InspectAsync(normalZip);
    Check(inspection.PostCount == 2 && inspection.ImageCount == 1, "Preview inspection uses the shared extraction and parsing core");

    var cleanupPaths = new AppPathSet(
        Path.Combine(root, "cleanup-data"),
        Path.Combine(root, "cleanup-reports"),
        Path.Combine(root, "cleanup-temp"));
    var failingCleanup = new FailOnceTemporaryDirectoryCleaner();
    var cleanupFailureSurfaced = false;
    try
    {
        await new ArchiveInspectionService(cleanupPaths, failingCleanup).InspectAsync(normalZip);
    }
    catch (IOException error)
    {
        cleanupFailureSurfaced = error.Message.Contains(cleanupPaths.TemporaryRoot, StringComparison.Ordinal);
    }
    var pendingWorkspace = Directory.Exists(cleanupPaths.TemporaryRoot)
        ? Directory.EnumerateDirectories(cleanupPaths.TemporaryRoot, "inspect-*", SearchOption.TopDirectoryOnly).SingleOrDefault()
        : null;
    Check(cleanupFailureSurfaced && pendingWorkspace is not null, "Temporary cleanup failures are surfaced and remain recoverable");

    var unrelatedDirectory = Path.Combine(cleanupPaths.TemporaryRoot, "unrelated");
    Directory.CreateDirectory(unrelatedDirectory);
    new ArchiveInspectionService(cleanupPaths).CleanupStaleWorkspaces();
    Check(
        pendingWorkspace is not null && !Directory.Exists(pendingWorkspace) && Directory.Exists(unrelatedDirectory),
        "The next startup removes only stale FB2Blogger inspection workspaces");

    var falsePositive = Path.Combine(cleanupPaths.TemporaryRoot, "inspect-user-data");
    Directory.CreateDirectory(falsePositive);
    File.WriteAllText(Path.Combine(falsePositive, "keep.txt"), "user-owned");
    var unmarkedGuidDirectory = Path.Combine(
        cleanupPaths.TemporaryRoot,
        "inspect-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(unmarkedGuidDirectory);
    File.WriteAllText(Path.Combine(unmarkedGuidDirectory, "keep.txt"), "not-marked-by-app");

    var workspaceManager = new InspectionWorkspaceManager(
        cleanupPaths,
        new FileSystemTemporaryDirectoryCleaner());
    var activeWorkspace = workspaceManager.CreateWorkspace();
    File.WriteAllText(Path.Combine(activeWorkspace.DirectoryPath, "private.json"), "active-private-data");
    new ArchiveInspectionService(cleanupPaths).CleanupStaleWorkspaces();
    Check(
        Directory.Exists(falsePositive) && Directory.Exists(unmarkedGuidDirectory),
        "Cleanup ignores inspect-user-data and unmarked GUID-shaped folders");
    Check(
        Directory.Exists(activeWorkspace.DirectoryPath),
        "Cleanup never removes a workspace held by another active lease");
    workspaceManager.ReleaseAndDelete(activeWorkspace);

    var childLeaseId = Guid.NewGuid();
    var childLeaseName = InspectionWorkspaceManager.WorkspaceName(childLeaseId);
    var childLeasePath = Path.Combine(
        cleanupPaths.TemporaryRoot,
        childLeaseName + InspectionWorkspaceManager.LeaseSuffix);
    var childWorkspacePath = Path.Combine(cleanupPaths.TemporaryRoot, childLeaseName);
    File.WriteAllText(
        childLeasePath,
        InspectionWorkspaceManager.LeaseContents(childLeaseId),
        new UTF8Encoding(false));
    Directory.CreateDirectory(childWorkspacePath);
    File.WriteAllText(
        Path.Combine(childWorkspacePath, InspectionWorkspaceManager.MarkerFileName),
        InspectionWorkspaceManager.MarkerContents,
        new UTF8Encoding(false));
    File.WriteAllText(Path.Combine(childWorkspacePath, "private.json"), "held-by-child-process");

    var childLeaseReady = false;
    using (var holder = StartLeaseHolder(childLeasePath))
    {
        try
        {
            var readyLine = await holder.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15));
            childLeaseReady = string.Equals(readyLine, "READY", StringComparison.Ordinal);
            Check(childLeaseReady, "A separate process acquired the inspection lease");
            if (childLeaseReady)
            {
                new ArchiveInspectionService(cleanupPaths).CleanupStaleWorkspaces();
                Check(
                    Directory.Exists(childWorkspacePath) && File.Exists(childLeasePath),
                    "Cross-process cleanup skips a workspace while FileShare.None is held");
            }
        }
        catch (TimeoutException)
        {
            Check(false, "A separate process acquired the inspection lease");
        }
        finally
        {
            try { await holder.StandardInput.WriteLineAsync("release"); }
            catch (Exception error) when (error is IOException or InvalidOperationException) { }

            try { await holder.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15)); }
            catch (TimeoutException)
            {
                if (!holder.HasExited) holder.Kill(entireProcessTree: true);
                await holder.WaitForExitAsync();
            }
        }
    }
    if (childLeaseReady)
    {
        new ArchiveInspectionService(cleanupPaths).CleanupStaleWorkspaces();
        Check(
            !Directory.Exists(childWorkspacePath) && !File.Exists(childLeasePath),
            "Cross-process cleanup removes the workspace after the lease is released");
    }

    var leaseBeforeDirectoryId = Guid.NewGuid();
    var leaseBeforeDirectoryName = InspectionWorkspaceManager.WorkspaceName(leaseBeforeDirectoryId);
    var leaseBeforeDirectoryPath = Path.Combine(
        cleanupPaths.TemporaryRoot,
        leaseBeforeDirectoryName + InspectionWorkspaceManager.LeaseSuffix);
    File.WriteAllText(
        leaseBeforeDirectoryPath,
        InspectionWorkspaceManager.LeaseContents(leaseBeforeDirectoryId),
        new UTF8Encoding(false));

    var directoryBeforeMarkerId = Guid.NewGuid();
    var directoryBeforeMarkerName = InspectionWorkspaceManager.WorkspaceName(directoryBeforeMarkerId);
    var directoryBeforeMarkerPath = Path.Combine(cleanupPaths.TemporaryRoot, directoryBeforeMarkerName);
    var directoryBeforeMarkerLease = Path.Combine(
        cleanupPaths.TemporaryRoot,
        directoryBeforeMarkerName + InspectionWorkspaceManager.LeaseSuffix);
    File.WriteAllText(
        directoryBeforeMarkerLease,
        InspectionWorkspaceManager.LeaseContents(directoryBeforeMarkerId),
        new UTF8Encoding(false));
    Directory.CreateDirectory(directoryBeforeMarkerPath);

    var leaseAfterDirectoryDeletionId = Guid.NewGuid();
    var leaseAfterDirectoryDeletionName = InspectionWorkspaceManager.WorkspaceName(leaseAfterDirectoryDeletionId);
    var leaseAfterDirectoryDeletionPath = Path.Combine(
        cleanupPaths.TemporaryRoot,
        leaseAfterDirectoryDeletionName + InspectionWorkspaceManager.LeaseSuffix);
    File.WriteAllText(
        leaseAfterDirectoryDeletionPath,
        InspectionWorkspaceManager.LeaseContents(leaseAfterDirectoryDeletionId),
        new UTF8Encoding(false));
    var deletedBeforeLeasePath = Path.Combine(cleanupPaths.TemporaryRoot, leaseAfterDirectoryDeletionName);
    Directory.CreateDirectory(deletedBeforeLeasePath);
    File.WriteAllText(
        Path.Combine(deletedBeforeLeasePath, InspectionWorkspaceManager.MarkerFileName),
        InspectionWorkspaceManager.MarkerContents,
        new UTF8Encoding(false));
    Directory.Delete(deletedBeforeLeasePath, true);

    var fakeLeaseId = Guid.NewGuid();
    var fakeLeaseName = InspectionWorkspaceManager.WorkspaceName(fakeLeaseId);
    var fakeLeasePath = Path.Combine(
        cleanupPaths.TemporaryRoot,
        fakeLeaseName + InspectionWorkspaceManager.LeaseSuffix);
    var fakeLeaseDirectory = Path.Combine(cleanupPaths.TemporaryRoot, fakeLeaseName);
    File.WriteAllText(fakeLeasePath, "not an FB2Blogger lease", new UTF8Encoding(false));
    Directory.CreateDirectory(fakeLeaseDirectory);
    File.WriteAllText(
        Path.Combine(fakeLeaseDirectory, InspectionWorkspaceManager.MarkerFileName),
        InspectionWorkspaceManager.MarkerContents,
        new UTF8Encoding(false));
    var looseFalsePositiveLease = Path.Combine(cleanupPaths.TemporaryRoot, "inspect-user-data.lease");
    File.WriteAllText(looseFalsePositiveLease, "user-owned", new UTF8Encoding(false));

    var invalidLeaseId = Guid.NewGuid();
    var invalidLeaseName = InspectionWorkspaceManager.WorkspaceName(invalidLeaseId);
    var invalidLeasePath = Path.Combine(
        cleanupPaths.TemporaryRoot,
        invalidLeaseName + InspectionWorkspaceManager.LeaseSuffix);
    var invalidLeaseDirectory = Path.Combine(cleanupPaths.TemporaryRoot, invalidLeaseName);
    File.WriteAllBytes(
        invalidLeasePath,
        Enumerable.Repeat(
            (byte)0xFF,
            Encoding.UTF8.GetByteCount(InspectionWorkspaceManager.LeaseContents(invalidLeaseId))).ToArray());
    Directory.CreateDirectory(invalidLeaseDirectory);
    File.WriteAllText(
        Path.Combine(invalidLeaseDirectory, InspectionWorkspaceManager.MarkerFileName),
        InspectionWorkspaceManager.MarkerContents,
        new UTF8Encoding(false));

    var oversizedLeaseId = Guid.NewGuid();
    var oversizedLeaseName = InspectionWorkspaceManager.WorkspaceName(oversizedLeaseId);
    var oversizedLeasePath = Path.Combine(
        cleanupPaths.TemporaryRoot,
        oversizedLeaseName + InspectionWorkspaceManager.LeaseSuffix);
    var oversizedLeaseDirectory = Path.Combine(cleanupPaths.TemporaryRoot, oversizedLeaseName);
    File.WriteAllBytes(oversizedLeasePath, new byte[1024 * 1024]);
    Directory.CreateDirectory(oversizedLeaseDirectory);
    File.WriteAllText(
        Path.Combine(oversizedLeaseDirectory, InspectionWorkspaceManager.MarkerFileName),
        InspectionWorkspaceManager.MarkerContents,
        new UTF8Encoding(false));

    new ArchiveInspectionService(cleanupPaths).CleanupStaleWorkspaces();
    Check(
        !File.Exists(leaseBeforeDirectoryPath),
        "Cleanup recovers a trusted lease created before its workspace directory");
    Check(
        !Directory.Exists(directoryBeforeMarkerPath) && !File.Exists(directoryBeforeMarkerLease),
        "Cleanup recovers a trusted directory created before its ownership marker");
    Check(
        !File.Exists(leaseAfterDirectoryDeletionPath),
        "Cleanup recovers a trusted lease left after workspace deletion");
    Check(
        File.Exists(fakeLeasePath) && Directory.Exists(fakeLeaseDirectory) && File.Exists(looseFalsePositiveLease),
        "Cleanup preserves untrusted strict-name leases and inspect-user-data lease false positives");
    Check(
        File.Exists(invalidLeasePath) && Directory.Exists(invalidLeaseDirectory) &&
        File.Exists(oversizedLeasePath) && Directory.Exists(oversizedLeaseDirectory),
        "Malformed and oversized lease files remain untrusted without aborting cleanup");
}
finally
{
    try { Directory.Delete(root, true); } catch { }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine("FAILED: " + string.Join(", ", failures));
    return 1;
}

Console.WriteLine("ALL CROSS-PLATFORM CORE TESTS PASSED");
return 0;

static Process StartLeaseHolder(string leasePath)
{
    var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Current process path is unavailable.");
    var startInfo = new ProcessStartInfo
    {
        FileName = executable,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    if (string.Equals(Path.GetFileNameWithoutExtension(executable), "dotnet", StringComparison.OrdinalIgnoreCase))
        startInfo.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
    startInfo.ArgumentList.Add("--hold-lease");
    startInfo.ArgumentList.Add(leasePath);

    var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the lease-holder process.");
    process.StandardInput.AutoFlush = true;
    return process;
}

sealed class FailOnceTemporaryDirectoryCleaner : ITemporaryDirectoryCleaner
{
    bool shouldFail = true;

    public void DeleteRecursively(string path)
    {
        if (shouldFail)
        {
            shouldFail = false;
            throw new IOException("Simulated cleanup failure");
        }
        Directory.Delete(path, true);
    }
}
