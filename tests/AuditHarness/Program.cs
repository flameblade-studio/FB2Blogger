using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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
void Check(bool condition, string name) { Console.WriteLine($"{(condition ? "PASS" : "FAIL")} {name}"); if (!condition) failures.Add(name); }
var assembly = Assembly.Load("FB2Blogger");
var coreAssembly = Assembly.Load("FB2Blogger.Core");

var expectedWorkflowBadges = new[]
{
    "actions/workflows/ci.yml/badge.svg",
    "actions/workflows/codeql.yml/badge.svg",
    "actions/workflows/security-audit.yml/badge.svg",
    "actions/workflows/secret-defense.yml/badge.svg"
};
var expectedBadgeNames = new[]
{
    "Cross-platform CI",
    "CodeQL",
    "Security Audit / NuGet",
    "Secret Defense / Gitleaks",
    "Latest release",
    "MIT",
    ".NET 10",
    "Four interface languages"
};
string? canonicalBadgeBlock = null;

foreach (var readmeName in new[] { "README.md", "README.zh-CN.md", "README.en.md", "README.ja.md" })
{
    var readme = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), readmeName));
    var badgeMatch = Regex.Match(readme, @"<p align=""center"">.*?</p>", RegexOptions.Singleline, TimeSpan.FromSeconds(1));
    Check(badgeMatch.Success, $"{readmeName} contains a badge block");
    var badgeBlock = badgeMatch.Value.Replace("\r\n", "\n", StringComparison.Ordinal);
    canonicalBadgeBlock ??= badgeBlock;
    Check(string.Equals(canonicalBadgeBlock, badgeBlock, StringComparison.Ordinal), $"{readmeName} uses the shared badge block");
    var badgeNames = Regex.Matches(badgeBlock, @"<img alt=""([^""]+)""").Cast<Match>().Select(match => match.Groups[1].Value).ToArray();
    Check(badgeNames.SequenceEqual(expectedBadgeNames), $"{readmeName} uses the canonical eight-badge order and names");
    foreach (var badgeUrl in expectedWorkflowBadges)
        Check(badgeBlock.Contains(badgeUrl, StringComparison.Ordinal), $"{readmeName} links the real {Path.GetFileNameWithoutExtension(badgeUrl.Replace("/badge.svg", "", StringComparison.Ordinal))} workflow status");
    Check(readme.Contains("img.shields.io/github/v/release/hitoshic1982/FB2Blogger", StringComparison.Ordinal), $"{readmeName} shows the latest release");
    Check(readme.Contains("license-MIT-blue.svg", StringComparison.Ordinal), $"{readmeName} shows the MIT license");
    Check(badgeBlock.Contains("img.shields.io/badge/.NET-10.0", StringComparison.Ordinal) && badgeBlock.Contains("interface%20languages-4", StringComparison.Ordinal), $"{readmeName} labels .NET 10 and four interface languages as static facts");
    Check(!Regex.IsMatch(badgeBlock, @"(?i)(?:version|release)-v?1\.|v1\.1\.0"), $"{readmeName} has no hard-coded legacy version badge");
    Check(readme.Contains("docs/RELEASE-PROCESS.md", StringComparison.Ordinal), $"{readmeName} links the shared Flameblade quality and release standard");
    Check(readme.Contains("https://ko-fi.com/flamebladestudio", StringComparison.Ordinal), $"{readmeName} links the verified Ko-fi support page");
    Check(!readme.Contains("buymeacoffee.com", StringComparison.OrdinalIgnoreCase) && !readme.Contains("paypal.com/paypalme", StringComparison.OrdinalIgnoreCase), $"{readmeName} excludes retired support links");
    Check(!readme.Contains("\n+<p align=\"center\">", StringComparison.Ordinal), $"{readmeName} has no stray patch marker");
}

var funding = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), ".github", "FUNDING.yml")).Replace("\r\n", "\n", StringComparison.Ordinal);
Check(funding == "ko_fi: flamebladestudio\n", "GitHub Sponsor button uses only the verified Ko-fi account");

var contributing = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "CONTRIBUTING.md"));
var releaseProcessPath = Path.Combine(Directory.GetCurrentDirectory(), "docs", "RELEASE-PROCESS.md");
Check(File.Exists(releaseProcessPath), "Four-language release process exists");
var releaseProcess = File.ReadAllText(releaseProcessPath);
const string qualityPattern = @"<!-- QUALITY-STANDARD:BEGIN -->.*?<!-- QUALITY-STANDARD:END -->";
var contributingStandard = Regex.Match(contributing, qualityPattern, RegexOptions.Singleline, TimeSpan.FromSeconds(1)).Value.Replace("\r\n", "\n", StringComparison.Ordinal);
var releaseStandard = Regex.Match(releaseProcess, qualityPattern, RegexOptions.Singleline, TimeSpan.FromSeconds(1)).Value.Replace("\r\n", "\n", StringComparison.Ordinal);
Check(contributingStandard.Length > 0 && string.Equals(contributingStandard, releaseStandard, StringComparison.Ordinal), "Contribution and release documents share one identical four-language quality standard");
foreach (var gate in new[] { "LANG-4", "REAL-CHECKS", "NO-SECRETS", "TRACEABLE", "NO-REGRESSION", "PLATFORM-TRUTH", "SYNC", "NO-ONE-OFF" })
    Check(Regex.Matches(releaseStandard, $@"\| `{Regex.Escape(gate)}` \|").Count == 1, $"Quality standard defines gate {gate} exactly once");
foreach (var declaration in new[]
{
    "劍，我已鍛成；餘下的路，就交給你們了。",
    "剑，我已锻成；余下的路，就交给你们了。",
    "I have forged this sword. What comes next is up to you.",
    "この剣は、私が鍛え上げました。あとは皆さんに託します。"
})
    Check(releaseStandard.Contains(declaration, StringComparison.Ordinal), $"Quality standard preserves the exact core declaration: {declaration}");
Check(
    Regex.Matches(releaseProcess, @"(?m)^## (?:繁體中文|简体中文|English|日本語)\r?$", RegexOptions.None, TimeSpan.FromSeconds(1)).Count == 4,
    "Release procedure provides all four language sections");

var workflowDirectory = Path.Combine(Directory.GetCurrentDirectory(), ".github", "workflows");
var workflowFiles = new[] { "ci.yml", "codeql.yml", "security-audit.yml", "secret-defense.yml", "dependency-review.yml" };
foreach (var workflowFile in workflowFiles)
    Check(File.Exists(Path.Combine(workflowDirectory, workflowFile)), $"Workflow exists: {workflowFile}");

var codeqlWorkflow = File.ReadAllText(Path.Combine(workflowDirectory, "codeql.yml"));
Check(
    codeqlWorkflow.Contains("languages: csharp", StringComparison.Ordinal) &&
    codeqlWorkflow.Contains("build-mode: manual", StringComparison.Ordinal) &&
    codeqlWorkflow.Contains("dotnet build FB2Blogger.slnx", StringComparison.Ordinal),
    "CodeQL analyzes C# after building the shared solution");

var secretDefenseWorkflow = File.ReadAllText(Path.Combine(workflowDirectory, "secret-defense.yml"));
Check(
    Regex.IsMatch(secretDefenseWorkflow, @"gitleaks/gitleaks-action@[0-9a-f]{40}(?:\r?$|\s)", RegexOptions.Multiline, TimeSpan.FromSeconds(1)),
    "Secret Defense pins Gitleaks to an immutable commit");

var securityAuditWorkflow = File.ReadAllText(Path.Combine(workflowDirectory, "security-audit.yml"));
Check(
    securityAuditWorkflow.Contains("NuGetAudit=true", StringComparison.Ordinal) &&
    securityAuditWorkflow.Contains("NuGetAuditMode=all", StringComparison.Ordinal) &&
    securityAuditWorkflow.Contains("WarningsAsErrors=NU1901%3BNU1902%3BNU1903%3BNU1904", StringComparison.Ordinal),
    "Security Audit fails for known vulnerable direct or transitive NuGet packages");

var dependencyReviewWorkflow = File.ReadAllText(Path.Combine(workflowDirectory, "dependency-review.yml"));
Check(
    Regex.IsMatch(dependencyReviewWorkflow, @"actions/dependency-review-action@[0-9a-f]{40}\s+# v5\.0\.0", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)) &&
    dependencyReviewWorkflow.Contains("fail-on-severity: moderate", StringComparison.Ordinal),
    "Dependency Review checks pull-request dependency changes");

foreach (var scheduledWorkflow in new[] { codeqlWorkflow, securityAuditWorkflow, secretDefenseWorkflow })
    Check(
        scheduledWorkflow.Contains("schedule:", StringComparison.Ordinal) &&
        scheduledWorkflow.Contains("workflow_dispatch:", StringComparison.Ordinal) &&
        scheduledWorkflow.Contains("concurrency:", StringComparison.Ordinal) &&
        scheduledWorkflow.Contains("contents: read", StringComparison.Ordinal),
        "Scheduled security workflow supports manual runs, concurrency, and read-only contents");

var privacy = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "PRIVACY.md"));
var privacySections = Regex.Split(privacy, @"(?m)^## ")
    .Skip(1)
    .Where(section => section.Length > 0)
    .ToArray();
Check(privacySections.Length == 4, "Privacy notice contains four language sections");
Check(
    privacySections.All(section => Regex.Matches(section, @"(?m)^### ").Count == 4),
    "All privacy languages use the same four-part structure");
Check(
    privacySections.All(section =>
        section.Contains("%LocalAppData%\\FB2Blogger", StringComparison.Ordinal) &&
        section.Contains("FB2Blogger Reports", StringComparison.Ordinal) &&
        section.Contains("Windows DPAPI", StringComparison.Ordinal) &&
        section.Contains("inspect-*", StringComparison.Ordinal)),
    "Every privacy language documents Windows state, reports, DPAPI, and preview cleanup");

var workflow = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), ".github", "workflows", "ci.yml"));
Check(
    !workflow.Contains("name: FB2Blogger-v1.1.0-Windows-x64", StringComparison.Ordinal),
    "CI artifact name is not hard-coded to version 1.1.0");
Check(
    workflow.Contains("github.ref_type", StringComparison.Ordinal) &&
    workflow.Contains("Project.PropertyGroup.Version", StringComparison.Ordinal) &&
    workflow.Contains("PR-$($env:PR_NUMBER)-validation", StringComparison.Ordinal),
    "CI derives tag and project versions while labeling PR artifacts as validation builds");
Check(
    workflow.Contains("concurrency:", StringComparison.Ordinal) && workflow.Contains("cancel-in-progress: true", StringComparison.Ordinal),
    "Cross-platform CI cancels superseded work on the same ref");
Check(
    Regex.Matches(workflow, @"(?m)^  push:\r?$").Count == 1 &&
    workflow.Contains("branches: [main]", StringComparison.Ordinal) &&
    workflow.Contains("'v[0-9]+.[0-9]+.[0-9]+'", StringComparison.Ordinal) &&
    workflow.Contains("'v[0-9]+.[0-9]+.[0-9]+-*'", StringComparison.Ordinal),
    "One push trigger covers main plus stable and prerelease semantic-version tags");

// Localization: every supported language must expose the same complete key set.
var localizer = coreAssembly.GetType("FB2Blogger.L", true)!;
var supportedCodes = ((IEnumerable)localizer.GetProperty("SupportedCodes", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(null)!).Cast<string>().ToArray();
Check(supportedCodes.SequenceEqual(new[] { "zh-TW", "zh-CN", "en", "ja" }), "Four interface languages are available in the intended order");
var keysMethod = localizer.GetMethod("Keys", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
var languageKeys = supportedCodes.ToDictionary(code => code, code => ((IEnumerable)keysMethod.Invoke(null, new object[] { code })!).Cast<string>().OrderBy(key => key).ToArray());
Check(languageKeys.Values.All(keys => keys.SequenceEqual(languageKeys["en"])), "All interface languages contain the same translation keys");
Check(languageKeys["en"].Length >= 120, "Localization covers setup, Google authorization, migration reports, and safety messages");
var configureLanguage = localizer.GetMethod("Configure", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
var languageProperty = localizer.GetProperty("Language", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
var translate = localizer.GetMethod("T", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
foreach (var code in supportedCodes)
{
    configureLanguage.Invoke(null, new object?[] { code });
    Check((string)languageProperty.GetValue(null)! == code, $"Language selection applies: {code}");
    Check(languageKeys[code].All(key => !string.Equals((string)translate.Invoke(null, new object[] { key, Array.Empty<object>() })!, key, StringComparison.Ordinal)), $"Every localization key resolves in {code}");
}
configureLanguage.Invoke(null, new object?[] { "unsupported" });
Check(supportedCodes.Contains((string)languageProperty.GetValue(null)!), "Unsupported language safely falls back to Windows language detection");

// Production user-facing East Asian text must live in Localization.cs.
// This prevents a future status, error, or safety message from silently
// becoming Traditional-Chinese-only again.
var sourceRoot = Path.Combine(Directory.GetCurrentDirectory(), "src");
var eastAsianLiteral = new Regex(@"""(?:\\.|[^""\\])*[\u3040-\u30ff\u3400-\u9fff](?:\\.|[^""\\])*""", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2));
var hardcodedUserText = new List<string>();
foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.EndsWith("Localization.cs", StringComparison.OrdinalIgnoreCase)))
{
    var source = File.ReadAllText(file);
    foreach (Match match in eastAsianLiteral.Matches(source))
    {
        var line = source.AsSpan(0, match.Index).Count('\n') + 1;
        hardcodedUserText.Add($"{Path.GetRelativePath(Directory.GetCurrentDirectory(), file)}:{line}");
    }
}
if (hardcodedUserText.Count > 0) Console.Error.WriteLine("Unlocalized user-facing text: " + string.Join(", ", hardcodedUserText));
Check(hardcodedUserText.Count == 0, "No production user-facing East Asian string bypasses the localization catalog");

var root = Path.Combine(Path.GetTempPath(), "FB2Blogger-Audit-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    // Parser: modern UTF-8, legacy UTF-8-as-Latin1, emoji, labels, media, ordering.
    var export = Path.Combine(root, "export"); Directory.CreateDirectory(export);
    var legacyText = Encoding.Latin1.GetString(Encoding.UTF8.GetBytes("舊版中文 😀 #舊標籤"));
    var posts = new object[] {
        new { timestamp = 1700000002L, data = new[] { new { post = legacyText } } },
        new { timestamp = 1700000001L, data = new[] { new { post = "繁體中文 😀 #標籤" } }, attachments = new[] { new { data = new[] { new { media = new { uri = "media/photo.jpg" } } } } } }
    };
    File.WriteAllText(Path.Combine(export, "your_posts_1.json"), JsonSerializer.Serialize(posts), new UTF8Encoding(false));
    var parser = coreAssembly.GetType("FB2Blogger.FacebookParser", true)!;
    var read = parser.GetMethod("Read", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
    var parsed = ((IEnumerable)read.Invoke(null, new object?[] { export, (Action<string>)(_ => { }), CancellationToken.None })!).Cast<object>().ToList();
    Check(parsed.Count == 2, "Facebook JSON parses two posts");
    var textProperty = parsed[0].GetType().GetProperty("Text")!;
    var labelsProperty = parsed[0].GetType().GetProperty("Labels")!;
    var mediaProperty = parsed[0].GetType().GetProperty("Media")!;
    Check((string)textProperty.GetValue(parsed[0])! == "繁體中文 😀 #標籤", "Modern Chinese and emoji preserved");
    Check(((IEnumerable)labelsProperty.GetValue(parsed[0])!).Cast<object>().Any(x => x.ToString() == "標籤"), "Hashtag becomes label");
    Check(((IEnumerable)mediaProperty.GetValue(parsed[0])!).Cast<object>().Count() == 1, "Image attachment detected");
    Check((string)textProperty.GetValue(parsed[1])! == "舊版中文 😀 #舊標籤", "Legacy mojibake repaired");

    // Safe ZIP extraction and zip-slip rejection.
    var mainForm = assembly.GetType("FB2Blogger.MainForm", true)!;
    var extract = mainForm.GetMethod("SafeExtract", BindingFlags.Static | BindingFlags.NonPublic)!;
    var normalZip = Path.Combine(root, "normal.zip");
    using (var z = ZipFile.Open(normalZip, ZipArchiveMode.Create)) { var e = z.CreateEntry("folder/ok.txt"); using var w = new StreamWriter(e.Open()); w.Write("ok"); }
    var normalOut = Path.Combine(root, "normal-out"); extract.Invoke(null, new object[] { normalZip, normalOut, CancellationToken.None });
    Check(File.ReadAllText(Path.Combine(normalOut, "folder", "ok.txt")) == "ok", "Normal ZIP extracts safely");
    var evilZip = Path.Combine(root, "evil.zip");
    using (var z = ZipFile.Open(evilZip, ZipArchiveMode.Create)) { var e = z.CreateEntry("../escape.txt"); using var w = new StreamWriter(e.Open()); w.Write("bad"); }
    var blocked = false;
    try { extract.Invoke(null, new object[] { evilZip, Path.Combine(root, "evil-out"), CancellationToken.None }); }
    catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { blocked = true; }
    Check(blocked && !File.Exists(Path.Combine(root, "escape.txt")), "ZIP path traversal blocked");
    var cancelled = false; using (var source = new CancellationTokenSource()) { source.Cancel(); try { extract.Invoke(null, new object[] { normalZip, Path.Combine(root, "cancel-out"), source.Token }); } catch (TargetInvocationException e) when (e.InnerException is OperationCanceledException) { cancelled = true; } }
    Check(cancelled, "ZIP extraction honors pause/cancellation");

    // The legacy WinForms cleaner and the Avalonia preview share one temp root,
    // but only the preview owner may interpret or remove inspect-* workspaces.
    var legacyCleanupRoot = Path.Combine(root, "shared-temp");
    Directory.CreateDirectory(legacyCleanupRoot);
    var appPathSetType = coreAssembly.GetType("FB2Blogger.AppPathSet", true)!;
    var cleanupPaths = Activator.CreateInstance(
        appPathSetType,
        Path.Combine(root, "cleanup-data"),
        Path.Combine(root, "cleanup-reports"),
        legacyCleanupRoot)!;
    var cleanupLegacyTemps = mainForm.GetMethod(
        "CleanupStaleTemps",
        BindingFlags.Static | BindingFlags.NonPublic,
        binder: null,
        types: new[] { appPathSetType },
        modifiers: null)!;
    var workspaceManager = coreAssembly.GetType("FB2Blogger.InspectionWorkspaceManager", true)!;
    var workspaceId = Guid.NewGuid();
    var workspaceName = (string)workspaceManager.GetMethod(
        "WorkspaceName",
        BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { workspaceId })!;
    var markerName = (string)workspaceManager.GetField(
        "MarkerFileName",
        BindingFlags.Static | BindingFlags.NonPublic)!.GetRawConstantValue()!;
    var markerContents = (string)workspaceManager.GetField(
        "MarkerContents",
        BindingFlags.Static | BindingFlags.NonPublic)!.GetRawConstantValue()!;
    var leaseSuffix = (string)workspaceManager.GetField(
        "LeaseSuffix",
        BindingFlags.Static | BindingFlags.NonPublic)!.GetRawConstantValue()!;
    var leaseContents = (string)workspaceManager.GetMethod(
        "LeaseContents",
        BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { workspaceId })!;
    var previewWorkspace = Path.Combine(legacyCleanupRoot, workspaceName);
    var previewLease = previewWorkspace + leaseSuffix;
    Directory.CreateDirectory(previewWorkspace);
    File.WriteAllText(Path.Combine(previewWorkspace, markerName), markerContents, new UTF8Encoding(false));
    File.WriteAllText(Path.Combine(previewWorkspace, "private.json"), "held-by-preview-process", new UTF8Encoding(false));
    File.WriteAllText(previewLease, leaseContents, new UTF8Encoding(false));

    var staleLegacyWorkspace = Path.Combine(legacyCleanupRoot, Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(staleLegacyWorkspace);
    File.WriteAllText(Path.Combine(staleLegacyWorkspace, "stale.json"), "legacy", new UTF8Encoding(false));

    var markerProtectedLegacyName = Guid.NewGuid().ToString("N");
    var markerProtectedLegacyWorkspace = Path.Combine(legacyCleanupRoot, markerProtectedLegacyName);
    Directory.CreateDirectory(markerProtectedLegacyWorkspace);
    File.WriteAllText(
        Path.Combine(markerProtectedLegacyWorkspace, markerName),
        markerContents,
        new UTF8Encoding(false));
    File.WriteAllText(markerProtectedLegacyWorkspace + leaseSuffix, "separate-owner", new UTF8Encoding(false));

    var leaseReady = false;
    using (var holder = StartLeaseHolder(previewLease))
    {
        try
        {
            var readyLine = await holder.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15));
            leaseReady = string.Equals(readyLine, "READY", StringComparison.Ordinal);
            Check(leaseReady, "A separate preview process acquired the inspection lease");
            if (leaseReady)
            {
                cleanupLegacyTemps.Invoke(null, new[] { cleanupPaths });
                Check(
                    Directory.Exists(previewWorkspace) && File.Exists(previewLease) &&
                    File.Exists(Path.Combine(previewWorkspace, "private.json")),
                    "WinForms legacy cleanup preserves a preview workspace held by another process");
                Check(
                    !Directory.Exists(staleLegacyWorkspace),
                    "WinForms legacy cleanup still removes a stale GUID workspace");
                Check(
                    Directory.Exists(markerProtectedLegacyWorkspace) &&
                    File.Exists(markerProtectedLegacyWorkspace + leaseSuffix),
                    "WinForms legacy cleanup preserves marker-and-lease workspaces even with a GUID-shaped name");
            }
        }
        catch (TimeoutException)
        {
            Check(false, "A separate preview process acquired the inspection lease");
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

    // New-article safety helpers: media type, cache invalidation, private paths,
    // YouTube description limit, and Blogger hidden identity marker.
    var isVideo = mainForm.GetMethod("IsVideoPath", BindingFlags.Static | BindingFlags.NonPublic)!;
    Check((bool)isVideo.Invoke(null, new object[] { "clip.MP4" })! && !(bool)isVideo.Invoke(null, new object[] { "photo.jpg" })!, "New article distinguishes video from image");
    var cacheFile = Path.Combine(root, "cache.jpg"); File.WriteAllBytes(cacheFile, [1, 2, 3]);
    var cacheMethod = mainForm.GetMethod("ComposerCacheKey", BindingFlags.Static | BindingFlags.NonPublic)!;
    var cache1 = (string)cacheMethod.Invoke(null, new object[] { cacheFile })!; File.AppendAllText(cacheFile, "changed");
    var cache2 = (string)cacheMethod.Invoke(null, new object[] { cacheFile })!;
    Check(cache1 != cache2, "Changed media invalidates upload cache");
    var postType = coreAssembly.GetType("FB2Blogger.FacebookPost", true)!; var mediaType = coreAssembly.GetType("FB2Blogger.MediaItem", true)!;
    var media = Activator.CreateInstance(mediaType, "private-video.mp4", true)!;
    var labelList = Activator.CreateInstance(typeof(List<>).MakeGenericType(typeof(string)))!;
    var mediaListType = typeof(List<>).MakeGenericType(mediaType); var emptyMediaList = Activator.CreateInstance(mediaListType)!;
    var post = Activator.CreateInstance(postType, "manual-test", "Title", new string('文', 6000), DateTimeOffset.Now, labelList, emptyMediaList)!;
    var description = (string)mainForm.GetMethod("YouTubeDescription", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new[] { post, media })!;
    Check(description.Length <= 5000 && !description.Contains(root, StringComparison.OrdinalIgnoreCase), "YouTube description is bounded and does not expose local path");
    var api = assembly.GetType("FB2Blogger.GoogleApi", true)!;
    var marker = (string)api.GetMethod("ExtractMigrationKey", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { "x<!-- FB2BLOGGER:manual-test -->y" })!;
    Check(marker == "manual-test", "Blogger hidden identity marker round-trips");

    // Smart image optimization: source integrity, resize/size reduction and
    // lossless/animation-safe format preservation.
    var optimizer = assembly.GetType("FB2Blogger.ImageOptimizer", true)!;
    var prepare = optimizer.GetMethod("Prepare", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
    var largeJpeg = Path.Combine(root, "large.jpg");
    using (var bitmap = new Bitmap(4000, 3000))
    {
        using var g = Graphics.FromImage(bitmap); g.Clear(Color.CornflowerBlue); var random = new Random(42);
        for (var i = 0; i < 8000; i++) using (var brush = new SolidBrush(Color.FromArgb(random.Next(256), random.Next(256), random.Next(256)))) g.FillEllipse(brush, random.Next(4000), random.Next(3000), random.Next(10, 180), random.Next(10, 180));
        bitmap.Save(largeJpeg, ImageFormat.Jpeg);
    }
    var originalHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(largeJpeg)));
    using (var optimized = (IDisposable)prepare.Invoke(null, new object[] { largeJpeg })!)
    {
        var optimizedType = optimized.GetType(); var optimizedPath = (string)optimizedType.GetProperty("Path")!.GetValue(optimized)!;
        using var image = Image.FromFile(optimizedPath);
        Check(Math.Max(image.Width, image.Height) <= 2560, "Large JPEG is resized for web reading");
        Check(new FileInfo(optimizedPath).Length < new FileInfo(largeJpeg).Length, "Large JPEG uses less Drive storage");
    }
    Check(File.Exists(largeJpeg) && Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(largeJpeg))) == originalHash, "Original image is never modified");
    var png = Path.Combine(root, "text.png"); using (var bitmap = new Bitmap(800, 400)) bitmap.Save(png, ImageFormat.Png);
    using (var preserved = (IDisposable)prepare.Invoke(null, new object[] { png })!) Check((string)preserved.GetType().GetProperty("Path")!.GetValue(preserved)! == png, "PNG is preserved without lossy recompression");
    var driveId = (string)mainForm.GetMethod("DriveIdFromUrl", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { "https://drive.google.com/thumbnail?id=ABC_123&sz=w1600" })!;
    Check(driveId == "ABC_123", "Existing Drive file ID is retained for in-place replacement");

    // Migration state atomic save/load and backup recovery.
    var store = assembly.GetType("FB2Blogger.SettingsStore", true)!;
    var stateType = coreAssembly.GetType("FB2Blogger.MigrationState", true)!;
    var state = Activator.CreateInstance(stateType)!;
    var fakeZip = Path.Combine(root, "identity.zip"); File.WriteAllText(fakeZip, "x");
    store.GetMethod("SaveMigration", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, new[] { fakeZip, state });
    var loaded = store.GetMethod("LoadMigration", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, new[] { fakeZip });
    Check(loaded is not null, "Migration state round-trips");
    var statePath = (string)store.GetMethod("DetailedStateFile", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, new[] { fakeZip })!;
    store.GetMethod("SaveMigration", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, new[] { fakeZip, state });
    File.WriteAllText(statePath, "corrupt");
    var recovered = store.GetMethod("LoadMigration", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, new[] { fakeZip });
    Check(recovered is not null, "Corrupt migration state recovers from backup");
    foreach (var suffix in new[] { "", ".bak", ".tmp" }) try { File.Delete(statePath + suffix); } catch { }
}
finally { try { Directory.Delete(root, true); } catch { } }

if (failures.Count > 0) { Console.Error.WriteLine("FAILED: " + string.Join(", ", failures)); return 1; }
Console.WriteLine("ALL AUDIT TESTS PASSED");
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
