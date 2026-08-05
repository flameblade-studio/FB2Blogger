# Contributing / 參與貢獻 / 参与贡献 / コントリビューション

<!-- QUALITY-STANDARD:BEGIN -->
## 炎劍開源軟體家族品質標準 / Flameblade Open Source Software Family Quality Standard

| Gate | 繁體中文 | 简体中文 | English | 日本語 |
|---|---|---|---|---|
| `LANG-4` | 使用者可見文件與發布說明須同步繁中、簡中、英文、日文。 | 用户可见文档与发布说明须同步繁中、简中、英文、日文。 | Keep user-facing documentation and release notes synchronized in Traditional Chinese, Simplified Chinese, English, and Japanese. | 利用者向け文書とリリースノートは、繁体字中国語・簡体字中国語・英語・日本語を同時に更新します。 |
| `REAL-CHECKS` | 只呈現實際執行的 CI 與安全掃描，不把徽章當作裝飾。 | 只展示实际运行的 CI 与安全扫描，不把徽章当作装饰。 | Show only CI and security scans that actually run; badges are evidence, not decoration. | 実際に動作する CI とセキュリティスキャンだけを表示し、バッジを飾りにしません。 |
| `NO-SECRETS` | 原始碼、紀錄、套件與發布資產不得含機密或個資。 | 源代码、日志、软件包与发布资产不得含机密或个人资料。 | Keep secrets and personal data out of source, logs, packages, and release assets. | ソース、ログ、パッケージ、配布物に機密情報や個人情報を含めません。 |
| `TRACEABLE` | 每項發布產物須能追溯至版本、提交與自動建置紀錄。 | 每项发布产物须可追溯到版本、提交与自动构建记录。 | Every release artifact must be traceable to its version, commit, and automated build record. | すべての配布物を、バージョン・コミット・自動ビルド記録まで追跡可能にします。 |
| `NO-REGRESSION` | 不以破壞既有正常功能換取新功能或測試通過。 | 不以破坏既有正常功能来换取新功能或测试通过。 | Never trade working behavior for a new feature or a passing check. | 新機能やテスト成功のために、既存の正常動作を壊しません。 |
| `PLATFORM-TRUTH` | 清楚區分 CI 驗證、預覽功能與真實硬體正式支援。 | 明确区分 CI 验证、预览功能与真实硬件正式支持。 | Distinguish CI validation, preview capability, and production support on real hardware. | CI 検証、プレビュー機能、実機での正式サポートを明確に区別します。 |
| `SYNC` | 使用者可見變更須同步程式、文件、Release 與官網。 | 用户可见变更须同步程序、文档、Release 与官网。 | Synchronize user-visible changes across the app, documentation, releases, and official website. | 利用者に見える変更は、アプリ・文書・Release・公式サイトへ同期します。 |
| `NO-ONE-OFF` | 拒絕無法重複的一次性手工作業，優先採用自動化與可重現流程。 | 拒绝无法重复的一次性手工作业，优先采用自动化与可复现流程。 | Reject non-repeatable one-off manual exceptions; prefer automation and reproducible procedures. | 再現できない一度限りの手作業を避け、自動化と再現可能な手順を優先します。 |

### 核心宣言 / 核心宣言 / Core declaration / コア宣言

> **繁體中文：** 劍，我已鍛成；餘下的路，就交給你們了。
>
> **简体中文：** 剑，我已锻成；余下的路，就交给你们了。
>
> **English:** I have forged this sword. What comes next is up to you.
>
> **日本語：** この剣は、私が鍛え上げました。あとは皆さんに託します。

<!-- QUALITY-STANDARD:END -->

## 繁體中文

歡迎 Issue 與 Pull Request。請先確認問題能在最新版重現，勿附上私人 Facebook ZIP 或憑證。跨平台核心修改請執行 `dotnet run --project tests/CoreHarness/CoreHarness.csproj -c Release` 與 Avalonia 預覽殼建置；Windows 完整版另須執行 `dotnet run --project tests/AuditHarness/AuditHarness.csproj -c Release`。封裝或發布變更還須執行 `dotnet run --project tests/PackagingAudit/PackagingAudit.csproj -c Release`，並等待原生 runner 的 DMG／AppImage 啟動檢查。PR 請說明問題、做法、使用者影響、測試作業系統與結果；若改變使用者可見行為，請同步更新繁中、簡中、英文、日文文件。CI 通過不可寫成 macOS／Linux 實機驗證。

## 简体中文

欢迎提交 Issue 和 Pull Request。请先在最新版复现问题，勿附上私人 Facebook ZIP 或凭证。跨平台核心修改必须运行 CoreHarness 并构建 Avalonia 预览界面；Windows 完整版还必须运行 AuditHarness。打包或发布变更还必须运行 PackagingAudit，并等待原生 runner 的 DMG／AppImage 启动检查。PR 请说明问题、解决方式、用户影响、测试系统与结果；影响用户行为时必须同步更新繁中、简中、英文、日文文档。CI 通过不得写成 macOS／Linux 实机验证。

## English

Issues and pull requests are welcome. Reproduce the problem on the latest version and never attach private Facebook archives or credentials. Cross-platform core changes must run CoreHarness and build the Avalonia preview; the full Windows edition must also run AuditHarness. Packaging or release changes must run PackagingAudit and wait for native-runner DMG/AppImage launch checks. A PR should explain the problem, approach, user impact, test operating systems, and results. User-visible changes must update Traditional Chinese, Simplified Chinese, English, and Japanese documentation together. Never describe CI as real macOS or Linux hardware validation.

## 日本語

Issue と Pull Request を歓迎します。最新版で再現を確認し、非公開の Facebook ZIP や認証情報を添付しないでください。共通コアの変更では CoreHarness と Avalonia プレビューのビルドを行い、Windows 完全版では AuditHarness も実行してください。パッケージまたは公開手順の変更では PackagingAudit も実行し、ネイティブ runner の DMG／AppImage 起動確認を待ってください。PR には問題、対応方法、利用者への影響、検証した OS と結果を記載し、利用者向けの変更では繁体字中国語・簡体字中国語・英語・日本語の文書を同時に更新してください。CI 成功を macOS／Linux 実機検証と表現してはいけません。

