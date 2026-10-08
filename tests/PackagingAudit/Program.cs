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
var windowsScript = Read(Path.Combine("packaging", "windows", "build-release.ps1"));
var releaseGateScript = Read(Path.Combine("packaging", "validate-release-tag.sh"));
var releaseAssetsScript = Read(Path.Combine("packaging", "verify-release-assets.sh"));
var metadataScript = Read(Path.Combine("packaging", "resolve-metadata.sh"));
var sbomScript = Read(Path.Combine("packaging", "generate-sbom.sh"));
var program = Read(Path.Combine("src", "FB2Blogger.Desktop", "Program.cs"));
var windowsProgram = Read(Path.Combine("src", "FB2Blogger", "Program.cs"));
var mainForm = Read(Path.Combine("src", "FB2Blogger", "MainForm.cs"));
var notice = Read(Path.Combine("packaging", "PREVIEW-NOTICE.txt"));
var previewDocument = Read(Path.Combine("docs", "PREVIEW-PACKAGES.md"));
var releaseNotes = Read(Path.Combine("docs", "releases", "v1.1.0-rc.1.md"));

Check(program.Contains("--package-smoke-test", StringComparison.Ordinal) &&
      program.Contains("FB2BLOGGER_PREVIEW_SMOKE_OK", StringComparison.Ordinal),
    "The packaged executable exposes a deterministic launch smoke test");

Check(windowsProgram.Contains("--package-smoke-test", StringComparison.Ordinal) &&
      windowsProgram.Contains("mainForm.Show()", StringComparison.Ordinal) &&
      windowsProgram.Contains("mainForm.IsHandleCreated", StringComparison.Ordinal) &&
      windowsProgram.Contains("FB2BLOGGER_WINDOWS_PACKAGE_SMOKE_OK", StringComparison.Ordinal) &&
      mainForm.Contains("enableInteractiveStartup", StringComparison.Ordinal),
    "The full Windows executable opens its real main window without triggering first-run interaction");
Check(windowsScript.Contains("Start-Process", StringComparison.Ordinal) &&
      windowsScript.Contains("-Wait", StringComparison.Ordinal) &&
      windowsScript.Contains("Get-FileHash", StringComparison.Ordinal) &&
      windowsScript.Contains("Microsoft.Sbom.DotNetTool", StringComparison.Ordinal) &&
      windowsScript.Contains("--version 4.1.5", StringComparison.Ordinal) &&
      windowsScript.Contains("CreateDirectory($manifestPath)", StringComparison.Ordinal) &&
      windowsScript.Contains("FB2Blogger-Windows-x64-LICENSE.txt", StringComparison.Ordinal) &&
      windowsScript.Contains("Permission is hereby granted", StringComparison.Ordinal),
    "Windows packaging executes the published EXE and creates a readable MIT license, SHA256, and pinned SPDX SBOM");

Check(macScript.Contains("hdiutil create", StringComparison.Ordinal) &&
      macScript.Contains("FB2Blogger Preview.app", StringComparison.Ordinal) &&
      macScript.Contains("Contents/MacOS/FB2Blogger.Desktop", StringComparison.Ordinal) &&
      macScript.Contains("bundle_short_version=\"${app_version%%-*}\"", StringComparison.Ordinal) &&
      macScript.Contains("hdiutil attach", StringComparison.Ordinal) &&
      macScript.Contains("--package-smoke-test", StringComparison.Ordinal) &&
      macScript.Contains("contents}/Resources/LICENSE.txt", StringComparison.Ordinal) &&
      macScript.Contains("dmg_root}/LICENSE.txt", StringComparison.Ordinal) &&
      macScript.Contains("Permission is hereby granted", StringComparison.Ordinal) &&
      macScript.Contains("shasum -a 256", StringComparison.Ordinal),
    "macOS packaging creates, mounts, launches, and hashes a real DMG whose root and app resources contain the MIT license");

Check(linuxScript.Contains("FB2Blogger.AppDir", StringComparison.Ordinal) &&
      linuxScript.Contains("appimagetool-x86_64.AppImage", StringComparison.Ordinal) &&
      linuxScript.Contains("--appimage-extract", StringComparison.Ordinal) &&
      linuxScript.Contains("--package-smoke-test", StringComparison.Ordinal) &&
      linuxScript.Contains("usr/share/doc/fb2blogger-preview/LICENSE.txt", StringComparison.Ordinal) &&
      linuxScript.Contains("Permission is hereby granted", StringComparison.Ordinal) &&
      linuxScript.Contains("sha256sum", StringComparison.Ordinal),
    "Linux packaging creates, extracts, launches, and hashes a real AppImage containing the MIT license");
Check(linuxScript.Contains("appimagetool/releases/download/1.9.1/appimagetool-x86_64.AppImage", StringComparison.Ordinal) &&
      linuxScript.Contains("ed4ce84f0d9caff66f50bcca6ff6f35aae54ce8135408b3fa33abfc3cb384eb0", StringComparison.Ordinal),
    "AppImage tooling is fail-closed to the tagged 1.9.1 release and its SHA256");

Check(metadataScript.Contains("REF_TYPE", StringComparison.Ordinal) &&
      metadataScript.Contains("PR-${PR_NUMBER", StringComparison.Ordinal) &&
      metadataScript.Contains("preview.build", StringComparison.Ordinal) &&
      metadataScript.Contains("RELEASE_TAG", StringComparison.Ordinal) &&
      Regex.Matches(workflow, @"RELEASE_TAG: \$\{\{ needs\.release-gate\.outputs\.tag \}\}").Count >= 4,
    "Artifact labels distinguish tags, PR validation, and main-branch preview builds");
Check(sbomScript.Contains("Microsoft.Sbom.DotNetTool", StringComparison.Ordinal) &&
      sbomScript.Contains("tool_version=\"4.1.5\"", StringComparison.Ordinal) &&
      sbomScript.Contains("manifest.spdx.json", StringComparison.Ordinal),
    "The Microsoft SBOM generator and output format are pinned");

Check(releaseGateScript.Contains("^v1\\.1\\.0-rc\\.([1-9][0-9]*)$", StringComparison.Ordinal) &&
      releaseGateScript.Contains("git merge-base --is-ancestor", StringComparison.Ordinal) &&
      releaseGateScript.Contains("release_tag=\"\"", StringComparison.Ordinal) &&
      releaseGateScript.Contains("release_tag=\"${ref_name}\"", StringComparison.Ordinal) &&
      releaseGateScript.Contains("build_commit=%s", StringComparison.Ordinal) &&
      workflow.Contains("+refs/heads/main:refs/remotes/origin/main", StringComparison.Ordinal),
    "Release authority requires an exact positive RC tag whose commit is contained in origin/main");
Check(workflow.Contains("permissions:\n  contents: read", StringComparison.Ordinal) &&
      workflow.Contains("if: needs.release-gate.outputs.publish == 'true'", StringComparison.Ordinal) &&
      workflow.Contains("Existing v1.1.0-rc.N tag to rebuild and publish", StringComparison.Ordinal) &&
      workflow.Contains("needs: [release-gate, windows-release, macos-x64-preview, macos-arm64-preview, linux-preview]", StringComparison.Ordinal),
    "Pull requests and ordinary main pushes remain read-only while release publication waits for every platform");
Check(workflow.Contains("name: Required - All platform packages", StringComparison.Ordinal) &&
      workflow.Contains("if: always() && github.event_name == 'pull_request'", StringComparison.Ordinal) &&
      workflow.Contains("needs: [windows-release, macos-x64-preview, macos-arm64-preview, linux-preview]", StringComparison.Ordinal) &&
      workflow.Contains("needs.macos-x64-preview.result", StringComparison.Ordinal) &&
      workflow.Contains("needs.macos-arm64-preview.result", StringComparison.Ordinal),
    "Every pull request exposes one stable required-check context aggregating all four package jobs");

foreach (var runner in new[] { "macos-15-intel", "macos-15", "ubuntu-24.04" })
    Check(workflow.Contains(runner, StringComparison.Ordinal), $"Workflow uses the explicit native runner {runner}");
foreach (var runtime in new[] { "osx-x64", "osx-arm64", "linux-x64" })
    Check(workflow.Contains(runtime, StringComparison.Ordinal), $"Workflow publishes the native runtime {runtime}");
Check(workflow.Contains("windows-2025", StringComparison.Ordinal) &&
      workflow.Contains("src/FB2Blogger/FB2Blogger.csproj", StringComparison.Ordinal) &&
      workflow.Contains("packaging/windows/build-release.ps1", StringComparison.Ordinal),
    "The same workflow builds the full Windows x64 release candidate");
Check(workflow.Contains("SBOM_DOTNET_SDK_VERSION: 8.0.419", StringComparison.Ordinal) &&
      Regex.Matches(workflow, @"\$\{\{ env\.SBOM_DOTNET_SDK_VERSION \}\}").Count == 4 &&
      workflow.Contains("DOTNET_SDK_VERSION: 10.0.204", StringComparison.Ordinal),
    "Every package runner installs the exact .NET 8 SBOM runtime alongside the pinned .NET 10 build SDK");

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
      workflow.Contains("if: needs.release-gate.outputs.publish == 'true'", StringComparison.Ordinal) &&
      workflow.Contains("id-token: write", StringComparison.Ordinal) &&
      workflow.Contains("attestations: write", StringComparison.Ordinal) &&
      workflow.Contains("artifact-metadata: write", StringComparison.Ordinal),
    "Attestations run only for trusted events with explicit minimum write permissions");
Check(workflow.Contains("sbom-path:", StringComparison.Ordinal) &&
      workflow.Contains("subject-path:", StringComparison.Ordinal),
    "Trusted builds bind both provenance and the SPDX SBOM to each package");
Check(releaseAssetsScript.Contains("FB2Blogger.exe", StringComparison.Ordinal) &&
      releaseAssetsScript.Contains("macOS-x64-Preview.dmg", StringComparison.Ordinal) &&
      releaseAssetsScript.Contains("macOS-arm64-Preview.dmg", StringComparison.Ordinal) &&
      releaseAssetsScript.Contains("Linux-x64-Preview.AppImage", StringComparison.Ordinal) &&
      releaseAssetsScript.Contains("FB2Blogger-Windows-x64-LICENSE.txt", StringComparison.Ordinal) &&
      releaseAssetsScript.Contains("Permission is hereby granted", StringComparison.Ordinal) &&
      releaseAssetsScript.Contains("sha256sum --check --strict", StringComparison.Ordinal) &&
      releaseAssetsScript.Contains("SHA256SUMS", StringComparison.Ordinal) &&
      workflow.Contains("bash packaging/verify-release-assets.sh", StringComparison.Ordinal),
    "The release job requires the exact four-platform inventory and creates aggregate SHA256SUMS");
Check(workflow.Contains("gh release create", StringComparison.Ordinal) &&
      workflow.Contains("--verify-tag", StringComparison.Ordinal) &&
      workflow.Contains("--draft", StringComparison.Ordinal) &&
      workflow.Contains("--prerelease", StringComparison.Ordinal) &&
      workflow.Contains("gh release create \"${RELEASE_TAG}\" \"${assets[@]}\"", StringComparison.Ordinal) &&
      !workflow.Contains("releases/tags/${RELEASE_TAG}", StringComparison.Ordinal) &&
      workflow.Contains("and .draft == true", StringComparison.Ordinal) &&
      workflow.Contains("draft=false", StringComparison.Ordinal) &&
      workflow.Contains("Release tag moved after validation", StringComparison.Ordinal) &&
      workflow.Contains("ref: ${{ needs.release-gate.outputs.build_commit }}", StringComparison.Ordinal) &&
      workflow.Contains("github.event_name == 'workflow_dispatch' && 'main' || needs.release-gate.outputs.build_commit", StringComparison.Ordinal) &&
      workflow.Contains("--notes-file", StringComparison.Ordinal),
    "The guarded workflow locks one reviewed commit and atomically publishes one exact curated GitHub pre-release with repaired recovery orchestration");
Check(workflow.Split("needs.release-gate.outputs.publish == 'true' && 'tag' || github.ref_type", StringSplitOptions.None).Length - 1 == 4 &&
      workflow.Split("needs.release-gate.outputs.publish == 'true' && needs.release-gate.outputs.tag || github.ref_name", StringSplitOptions.None).Length - 1 == 4,
    "Every package job presents a validated recovery run to immutable-tag-era metadata scripts as the exact RC tag");
var releaseGateSection = workflow[(workflow.IndexOf("release-gate:", StringComparison.Ordinal))..workflow.IndexOf("windows-release:", StringComparison.Ordinal)];
Check(releaseGateSection.Contains("REF_TYPE: ${{ github.ref_type }}", StringComparison.Ordinal) &&
      !releaseGateSection.Contains("needs.release-gate.outputs.publish", StringComparison.Ordinal),
    "The release authority never self-references downstream outputs while validating the requested tag");
foreach (var (packageJob, nextJob) in new[]
{
    ("windows-release:", "macos-x64-preview:"),
    ("macos-x64-preview:", "macos-arm64-preview:"),
    ("macos-arm64-preview:", "linux-preview:"),
    ("linux-preview:", "required-platform-packages:")
})
{
    var start = workflow.IndexOf(packageJob, StringComparison.Ordinal);
    var end = workflow.IndexOf(nextJob, start + packageJob.Length, StringComparison.Ordinal);
    var section = workflow[start..end];
    Check(section.Contains("needs.release-gate.outputs.publish == 'true' && 'tag' || github.ref_type", StringComparison.Ordinal),
        $"{packageJob.TrimEnd(':')} receives the validated RC identity during recovery");
}
Check(workflow.Contains("*.dmg", StringComparison.Ordinal) &&
      workflow.Contains("*.AppImage", StringComparison.Ordinal) &&
      !Regex.IsMatch(workflow + macScript + linuxScript, @"(?i)zip[^\n]*(?:\.dmg|\.AppImage)"),
    "The workflow produces native package formats instead of renamed ZIP files");

foreach (var heading in new[] { "繁體中文", "简体中文", "English", "日本語" })
{
    Check(notice.Contains(heading, StringComparison.Ordinal), $"In-package notice includes {heading}");
    Check(Regex.IsMatch(previewDocument, $@"(?m)^## {Regex.Escape(heading)}\r?$"), $"Preview guide includes {heading}");
    Check(Regex.IsMatch(releaseNotes, $@"(?m)^## {Regex.Escape(heading)}\r?$"), $"v1.1.0-rc.1 release notes include {heading}");
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
