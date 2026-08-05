using System.Text.RegularExpressions;

var failures = new List<string>();
void Check(bool condition, string name)
{
    Console.WriteLine($"{(condition ? "PASS" : "FAIL")} {name}");
    if (!condition) failures.Add(name);
}

string Read(string relativePath) => File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relativePath));

var workflow = Read(Path.Combine(".github", "workflows", "preview-packages.yml"));
var macScript = Read(Path.Combine("packaging", "macos", "build-dmg.sh"));
var linuxScript = Read(Path.Combine("packaging", "linux", "build-appimage.sh"));
var metadataScript = Read(Path.Combine("packaging", "resolve-metadata.sh"));
var sbomScript = Read(Path.Combine("packaging", "generate-sbom.sh"));
var program = Read(Path.Combine("src", "FB2Blogger.Desktop", "Program.cs"));
var notice = Read(Path.Combine("packaging", "PREVIEW-NOTICE.txt"));
var previewDocument = Read(Path.Combine("docs", "PREVIEW-PACKAGES.md"));

Check(program.Contains("--package-smoke-test", StringComparison.Ordinal) &&
      program.Contains("FB2BLOGGER_PREVIEW_SMOKE_OK", StringComparison.Ordinal),
    "The packaged executable exposes a deterministic launch smoke test");

Check(macScript.Contains("hdiutil create", StringComparison.Ordinal) &&
      macScript.Contains("FB2Blogger Preview.app", StringComparison.Ordinal) &&
      macScript.Contains("Contents/MacOS/FB2Blogger.Desktop", StringComparison.Ordinal) &&
      macScript.Contains("bundle_short_version=\"${app_version%%-*}\"", StringComparison.Ordinal) &&
      macScript.Contains("hdiutil attach", StringComparison.Ordinal) &&
      macScript.Contains("--package-smoke-test", StringComparison.Ordinal) &&
      macScript.Contains("shasum -a 256", StringComparison.Ordinal),
    "macOS packaging creates, mounts, launches, and hashes a real DMG containing an app bundle");

Check(linuxScript.Contains("FB2Blogger.AppDir", StringComparison.Ordinal) &&
      linuxScript.Contains("appimagetool-x86_64.AppImage", StringComparison.Ordinal) &&
      linuxScript.Contains("--appimage-extract", StringComparison.Ordinal) &&
      linuxScript.Contains("--package-smoke-test", StringComparison.Ordinal) &&
      linuxScript.Contains("sha256sum", StringComparison.Ordinal),
    "Linux packaging creates, extracts, launches, and hashes a real AppImage");
Check(linuxScript.Contains("8c8c91f762b412a19f4e8d2c4b35afb98f2d7c81", StringComparison.Ordinal) &&
      linuxScript.Contains("a6d71e2b6cd66f8e8d16c37ad164658985e0cf5fcaa950c90a482890cb9d13e0", StringComparison.Ordinal),
    "AppImage tooling is fail-closed to an audited upstream commit and SHA256");

Check(metadataScript.Contains("REF_TYPE", StringComparison.Ordinal) &&
      metadataScript.Contains("PR-${PR_NUMBER", StringComparison.Ordinal) &&
      metadataScript.Contains("preview.build", StringComparison.Ordinal),
    "Artifact labels distinguish tags, PR validation, and main-branch preview builds");
Check(sbomScript.Contains("Microsoft.Sbom.DotNetTool", StringComparison.Ordinal) &&
      sbomScript.Contains("tool_version=\"4.1.5\"", StringComparison.Ordinal) &&
      sbomScript.Contains("manifest.spdx.json", StringComparison.Ordinal),
    "The Microsoft SBOM generator and output format are pinned");

foreach (var runner in new[] { "macos-15-intel", "macos-15", "ubuntu-24.04" })
    Check(workflow.Contains(runner, StringComparison.Ordinal), $"Workflow uses the explicit native runner {runner}");
foreach (var runtime in new[] { "osx-x64", "osx-arm64", "linux-x64" })
    Check(workflow.Contains(runtime, StringComparison.Ordinal), $"Workflow publishes the native runtime {runtime}");

var actionUses = Regex.Matches(workflow, @"(?m)^\s*(?:-\s*)?uses:\s*([^\s]+)@([^\s]+)")
    .Cast<Match>()
    .Select(match => (Name: match.Groups[1].Value, Revision: match.Groups[2].Value))
    .ToArray();
Check(actionUses.Length >= 8, "Preview workflow invokes the expected official Actions");
Check(actionUses.All(action => Regex.IsMatch(action.Revision, "^[0-9a-f]{40}$", RegexOptions.CultureInvariant)),
    "Every GitHub Action is pinned to an immutable commit");
Check(actionUses.All(action => action.Name.StartsWith("actions/", StringComparison.Ordinal)),
    "Preview workflow uses only GitHub-maintained Actions");

Check(workflow.Contains("actions/attest@1e69f48acb82d1966a394da916b4c1698aa569d6", StringComparison.Ordinal) &&
      workflow.Contains("if: github.event_name != 'pull_request'", StringComparison.Ordinal) &&
      workflow.Contains("id-token: write", StringComparison.Ordinal) &&
      workflow.Contains("attestations: write", StringComparison.Ordinal) &&
      workflow.Contains("artifact-metadata: write", StringComparison.Ordinal),
    "Attestations run only for trusted events with explicit minimum write permissions");
Check(workflow.Contains("sbom-path:", StringComparison.Ordinal) &&
      workflow.Contains("subject-path:", StringComparison.Ordinal),
    "Trusted builds bind both provenance and the SPDX SBOM to each package");
Check(workflow.Contains("*.dmg", StringComparison.Ordinal) &&
      workflow.Contains("*.AppImage", StringComparison.Ordinal) &&
      !Regex.IsMatch(workflow + macScript + linuxScript, @"(?i)zip[^\n]*(?:\.dmg|\.AppImage)"),
    "The workflow produces native package formats instead of renamed ZIP files");

foreach (var heading in new[] { "繁體中文", "简体中文", "English", "日本語" })
{
    Check(notice.Contains(heading, StringComparison.Ordinal), $"In-package notice includes {heading}");
    Check(Regex.IsMatch(previewDocument, $@"(?m)^## {Regex.Escape(heading)}\r?$"), $"Preview guide includes {heading}");
}

foreach (var readme in new[] { "README.md", "README.zh-CN.md", "README.en.md", "README.ja.md" })
{
    var text = Read(readme);
    Check(text.Contains("docs/PREVIEW-PACKAGES.md", StringComparison.Ordinal), $"{readme} links the shared preview package guide");
}

if (failures.Count > 0)
{
    Console.Error.WriteLine("FAILED: " + string.Join(", ", failures));
    return 1;
}

Console.WriteLine("ALL PACKAGING AUDITS PASSED");
return 0;
