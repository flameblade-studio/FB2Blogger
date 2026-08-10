# FB2Blogger
<p align="center">
  <a href="https://github.com/hitoshic1982/FB2Blogger/actions/workflows/ci.yml"><img alt="Cross-platform CI" src="https://github.com/hitoshic1982/FB2Blogger/actions/workflows/ci.yml/badge.svg"></a>
  <a href="https://github.com/hitoshic1982/FB2Blogger/actions/workflows/codeql.yml"><img alt="CodeQL" src="https://github.com/hitoshic1982/FB2Blogger/actions/workflows/codeql.yml/badge.svg"></a>
  <a href="https://github.com/hitoshic1982/FB2Blogger/actions/workflows/security-audit.yml"><img alt="Security Audit / NuGet" src="https://github.com/hitoshic1982/FB2Blogger/actions/workflows/security-audit.yml/badge.svg"></a>
  <a href="https://github.com/hitoshic1982/FB2Blogger/actions/workflows/secret-defense.yml"><img alt="Secret Defense / Gitleaks" src="https://github.com/hitoshic1982/FB2Blogger/actions/workflows/secret-defense.yml/badge.svg"></a>
  <a href="https://github.com/hitoshic1982/FB2Blogger/releases"><img alt="Latest release" src="https://img.shields.io/github/v/release/hitoshic1982/FB2Blogger?label=release"></a>
  <a href="LICENSE"><img alt="MIT" src="https://img.shields.io/badge/license-MIT-blue.svg"></a>
  <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&amp;logoColor=white">
  <img alt="Four interface languages" src="https://img.shields.io/badge/interface%20languages-4-informational">
</p>

> This project follows the [Flameblade Open Source Software Family Quality Standard](docs/RELEASE-PROCESS.md): real checks, four-language synchronization, traceable releases, and no regressions.

[繁體中文](README.md) · [简体中文](README.zh-CN.md) · [English](README.en.md) · [日本語](README.ja.md)

A Windows desktop tool that moves posts, images, and videos from an official Facebook data export to Google Blogger.

> Your work should not live only on a platform whose rules can change without warning. FB2Blogger helps creators bring years of content back to a searchable, backup-friendly website they can manage for the long term.

## Why this project exists

In July 2026, after a Facebook Page operated by Flameblade Studio was disabled, its creator decided not to leave the fate of his digital work to a single platform. With ChatGPT Codex, he built migration tools and established a Blogger backup site and a WordPress home within three days. FB2Blogger grew out of that digital-sovereignty recovery effort.

The app reads a ZIP that you obtained through Facebook's official “Download your information” feature. It does not sign in to, scrape, or bypass Facebook, and it cannot restore a disabled account or Page.

## Features

- Parses post JSON from official Facebook exports, including repair for some legacy encoding damage.
- Preserves dates, text, emoji, and hashtags; hashtags become Blogger labels.
- Optimizes images safely before uploading them to Google Drive; videos can be uploaded to YouTube.
- Can create drafts so you can review every post before publication.
- Uses a hidden migration marker to prevent duplicate imports.
- Saves resumable progress and recovers corrupted progress data from a backup.
- Blocks ZIP path traversal and never modifies the source ZIP or original images.

## Platform status

- **Windows:** the existing full WinForms edition keeps its current features, LocalAppData location, and DPAPI credential protection.
- **macOS/Linux:** an Avalonia foundation preview can select, safely extract, and inspect a Facebook ZIP offline. Native CI produces macOS `.dmg` and Linux `.AppImage` preview packages. Google authorization, media upload, and Blogger publishing are not connected, so these are not production editions.
- **Shared foundation:** models, parsing, safe extraction, post-content composition, migration state, and platform data paths now live in `FB2Blogger.Core`, which GitHub Actions builds and tests separately on Windows, macOS, and Linux.

Passing CI is not evidence of real macOS or Linux hardware validation. The project owner currently has only Windows hardware, so production support still requires documented testing from contributors on those systems. See the [cross-platform foundation document](docs/CROSS-PLATFORM.md) for the exact matrix and security plan, and the [native preview package guide](docs/PREVIEW-PACKAGES.md) for downloads, hashes, attestations, and launch instructions.

## Requirements

1. Windows 10/11 x64.
2. Your own Facebook data export in JSON format, including posts and the media you want to migrate.
3. A Google Cloud Desktop OAuth client with Blogger API v3 and Google Drive API enabled. Enable YouTube Data API v3 only if you migrate videos.
4. Download the latest `FB2Blogger.exe` from [GitHub Releases](https://github.com/hitoshic1982/FB2Blogger/releases/latest) and verify its SHA256.

## Basic workflow

1. Start the app and enter your own Google OAuth Client ID. Enter a Client Secret only when your OAuth configuration requires one.
2. Select the Facebook ZIP.
3. Select the destination Blogger site and choose draft or published mode.
4. Start the migration, review the report, and spot-check posts, media, and dates.

Start with a test blog or draft mode. Google API quotas, variations in Facebook exports, and large media libraries may require multiple sessions.

## Privacy and security

- OAuth credentials and refresh tokens are stored only in the current Windows user's LocalAppData and encrypted with Windows DPAPI.
- The repository contains no author credentials, tokens, Facebook exports, or private content databases.
- The app communicates directly with Google APIs. Flameblade Studio operates no relay server that receives your content.

See [PRIVACY.md](PRIVACY.md) and [SECURITY.md](SECURITY.md).

## Build from source

```powershell
dotnet build src/FB2Blogger.Core/FB2Blogger.Core.csproj -c Release
dotnet run --project tests/CoreHarness/CoreHarness.csproj -c Release
dotnet build src/FB2Blogger.Desktop/FB2Blogger.Desktop.csproj -c Release
dotnet build src/FB2Blogger/FB2Blogger.csproj -c Release
dotnet run --project tests/AuditHarness/AuditHarness.csproj -c Release
dotnet run --project tests/PackagingAudit/PackagingAudit.csproj -c Release
```

The .NET 10 SDK pinned by `global.json` is required. Production releases remain self-contained Windows executables. A macOS or Linux artifact may be downloadable from Actions or attached to a release candidate only when it is clearly marked Preview; it does not contain the full Windows migration workflow.

## Open source and responsibility

Licensed under the [MIT License](LICENSE). Contributions and security-minded reviews are welcome.

This independent project is not affiliated with or endorsed by Meta, Facebook, Google, or Blogger. Migrate only content you are authorized to handle, and comply with platform terms, copyright, and privacy laws.

## Voluntary support

Every FB2Blogger migration feature remains free under the MIT License. Donations never unlock or restrict functionality. If the app helped preserve your writing or saved hours of manual work, you may voluntarily support Flameblade Studio's continued open-source maintenance:

- [Support on Ko-fi (one-time or monthly)](https://ko-fi.com/flamebladestudio)

Support is never required. Bug reports, documentation improvements, and pull requests are equally valuable contributions.

Author: CHOU MING HUA / Flameblade Studio · [Official website](https://www.flamebladestudio.com.tw/)

