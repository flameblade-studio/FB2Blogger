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

> 本專案遵守[炎劍開源軟體家族品質標準](docs/RELEASE-PROCESS.md)：真實驗證、四語同步、可追溯發布，且不犧牲既有功能。

[繁體中文](README.md) · [简体中文](README.zh-CN.md) · [English](README.en.md) · [日本語](README.ja.md)

把 Facebook 官方下載資料中的貼文、圖片與影片，整理並移轉到 Google Blogger 的 Windows 桌面工具。

> 內容不該只住在一個隨時可能改變規則的平台。FB2Blogger 協助創作者把多年累積的內容帶回可搜尋、可備份、可長期經營的網站。

## 這是什麼

2026 年 7 月，炎劍文化工作室的創作者在 Facebook 粉絲專頁遭停權後，決定不再把數位資產的命運交給單一平台。透過 ChatGPT Codex，三天內完成內容移轉工具、Blogger 備援站與 WordPress 品牌主站。FB2Blogger 正是這次數位主權自救行動所誕生的工具之一。

它讀取你本人透過 Facebook「下載你的資訊」取得的 ZIP，不會登入、爬取或繞過 Facebook，也無法恢復遭停權的帳號或粉絲專頁。

## 主要功能

- 解析 Facebook 官方匯出的貼文 JSON，包括新版 UTF-8 與部分舊版亂碼格式。
- 保留文章時間、文字、Emoji、Hashtag，並將 Hashtag 轉為 Blogger 標籤。
- 圖片經安全最佳化後上傳 Google Drive；影片可上傳 YouTube。
- 可先建立為草稿，讓你逐篇檢查後再公開。
- 以隱藏識別碼避免同一篇文章重複匯入。
- 記錄每篇移轉進度；中斷後可安全接續，損壞的進度檔可由備份復原。
- 防止惡意 ZIP 路徑穿越，不會修改原始 Facebook ZIP 或原圖。

## 三平台進度

- **Windows：**既有 WinForms 完整版維持原功能、原 LocalAppData 路徑與 DPAPI 憑證保護。
- **macOS／Linux：**目前是 Avalonia 基礎預覽殼，可離線選擇、安全解壓並解析 Facebook ZIP；原生 CI 會產生 macOS `.dmg` 與 Linux `.AppImage` 預覽封裝。Google 授權、媒體上傳與 Blogger 發布尚未接通，仍不是正式成品。
- **共同基礎：**解析、模型、安全解壓、文章內容組合、搬移進度與平台資料路徑已抽成 `FB2Blogger.Core`，由 GitHub Actions 在 Windows、macOS、Linux 分別建置與測試。

CI 通過不等於 macOS／Linux 實機驗證。專案擁有者目前只有 Windows 電腦，其他平台正式發布前仍需工程師提供真實環境測試。完整功能矩陣與安全計畫請見[跨平台基礎文件](docs/CROSS-PLATFORM.md)，下載、雜湊、來源證明與開啟方式請見[原生預覽封裝指南](docs/PREVIEW-PACKAGES.md)。

## 使用前準備

1. Windows 10/11 x64。
2. 從 Facebook 下載自己的資料，建議選 JSON 格式並包含貼文、相片與影片。
3. 在 Google Cloud 建立 Desktop OAuth Client，啟用 Blogger API v3、Google Drive API；需要移轉影片時再啟用 YouTube Data API v3。
4. 從 [GitHub Releases](https://github.com/hitoshic1982/FB2Blogger/releases/latest) 下載最新版 `FB2Blogger.exe`，並核對 SHA256。

## 基本流程

1. 啟動程式，輸入你自己的 Google OAuth Client ID；Client Secret 僅在你的 OAuth 設定需要時填寫。
2. 選擇 Facebook ZIP。
3. 選擇目標 Blogger 網誌與公開／草稿模式。
4. 開始移轉，完成後查看程式產生的報告並抽查文章、圖片、影片與日期。

請先用測試網誌或草稿模式驗證。Google API 配額、Facebook 匯出格式差異及大量媒體上傳，都可能需要分批完成。

## 隱私與安全

- OAuth 憑證與更新權杖只存放在目前 Windows 使用者的 LocalAppData，並以 Windows DPAPI 加密。
- 專案不附帶作者的 Client ID、Client Secret、Token、Facebook 匯出資料或文章資料庫。
- 軟體直接與 Google API 溝通；炎劍文化工作室不架設中繼伺服器收取你的內容。
- 使用者仍應自行保管 Facebook ZIP、Google 帳號及 OAuth 憑證。

詳見 [PRIVACY.md](PRIVACY.md) 與 [SECURITY.md](SECURITY.md)。

## 從原始碼建置

```powershell
dotnet build src/FB2Blogger.Core/FB2Blogger.Core.csproj -c Release
dotnet run --project tests/CoreHarness/CoreHarness.csproj -c Release
dotnet build src/FB2Blogger.Desktop/FB2Blogger.Desktop.csproj -c Release
dotnet build src/FB2Blogger/FB2Blogger.csproj -c Release
dotnet run --project tests/AuditHarness/AuditHarness.csproj -c Release
dotnet run --project tests/PackagingAudit/PackagingAudit.csproj -c Release
dotnet publish src/FB2Blogger/FB2Blogger.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts
```

需要固定於 `global.json` 的 .NET 10 SDK。現有正式 Release 仍提供 Windows 自含式單一 EXE；macOS／Linux 成品即使可由 Actions 下載或隨候選版附上，也必須清楚標示 Preview，且不具備 Windows 完整搬家功能。

## 開源與責任界線

本專案採 [MIT License](LICENSE)。歡迎檢查原始碼、回報問題及提交 PR。

這是獨立開源工具，與 Meta、Facebook、Google 或 Blogger 無隸屬或背書關係。請只移轉你有權處理的內容，並遵守各平台條款、著作權與個資法規。

## 自由贊助

FB2Blogger 的所有搬家功能都依 MIT 授權免費開放，不會因為是否贊助而限制功能。如果它幫你保住珍貴文章、減少手動整理時間，歡迎自由支持炎劍文化工作室繼續維護開源工具：

- [Ko-fi 贊助（單次或每月）](https://ko-fi.com/flamebladestudio)

不贊助也完全沒關係；回報問題、改善文件或提交 PR，同樣是重要的支持。

作者：CHOU MING HUA／炎劍文化工作室 · [官方網站](https://www.flamebladestudio.com.tw/)

