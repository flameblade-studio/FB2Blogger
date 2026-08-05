# Release process / 發行流程 / 发布流程 / リリース手順

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

1. 確認版本、四語文件、變更紀錄與官網同步內容。
2. 在乾淨工作樹執行完整建置、CoreHarness、AuditHarness、PackagingAudit、Windows 封裝、原生預覽封裝啟動檢查、NuGet 弱點稽核與 Gitleaks。
3. 由 Pull Request 合併，不直接推送受保護的 `main`。
4. 等待固定名稱的 `Required - All platform packages` 聚合檢查，以及 Cross-platform CI、CodeQL、Dependency Review、Security Audit 與 Secret Defense 全部通過；聚合檢查同時要求 Windows、Mac Intel、Mac Apple Silicon、Linux 封裝成功，可直接列入 `main` required checks。
5. `v1.1.0-rc.N` 候選版只接受 N 大於零、且標籤提交位於 `origin/main` 歷史中的 tag；PR、一般 `main` push 與手動工作流程只驗證，不發布。
6. Windows 完整 EXE、Mac Intel／Apple Silicon DMG、Linux x64 AppImage 必須在同一工作流程全部成功，再核對精確清單、各成品內可讀的 MIT 授權、個別 SHA256、彙總 `SHA256SUMS`、SPDX SBOM、四語說明與 GitHub 證明後，原子化建立 Pre-release。官網由既有 Flameblade Series Gateway 每小時同步 GitHub Releases，不另寫一次性頁面覆蓋程式。

## 简体中文

1. 确认版本、四语文档、变更记录与官网同步内容。
2. 在干净工作树运行完整构建、CoreHarness、AuditHarness、PackagingAudit、Windows 打包、原生预览软件包启动检查、NuGet 漏洞审计与 Gitleaks。
3. 通过 Pull Request 合并，不直接推送受保护的 `main`。
4. 等待固定名称的 `Required - All platform packages` 聚合检查，以及 Cross-platform CI、CodeQL、Dependency Review、Security Audit 与 Secret Defense 全部通过；聚合检查同时要求 Windows、Mac Intel、Mac Apple Silicon、Linux 打包成功，可直接加入 `main` required checks。
5. `v1.1.0-rc.N` 候选版只接受 N 大于零、且标签提交属于 `origin/main` 历史的 tag；PR、普通 `main` push 与手动工作流只验证，不发布。
6. Windows 完整 EXE、Mac Intel／Apple Silicon DMG、Linux x64 AppImage 必须在同一工作流中全部成功，再核对精确清单、各成品内可读的 MIT 许可证、单独 SHA256、汇总 `SHA256SUMS`、SPDX SBOM、四语说明与 GitHub 证明后，原子化建立 Pre-release。官网由现有 Flameblade Series Gateway 每小时同步 GitHub Releases，不另写一次性页面覆盖程序。

## English

1. Confirm the version, four-language documentation, changelog, and official-site synchronization.
2. From a clean worktree, run the full build, CoreHarness, AuditHarness, PackagingAudit, Windows publish, native preview package launch checks, NuGet vulnerability audit, and Gitleaks.
3. Merge through a pull request; never push directly to protected `main`.
4. Wait for the stable `Required - All platform packages` aggregate check, Cross-platform CI, CodeQL, Dependency Review, Security Audit, and Secret Defense. The aggregate requires Windows, Intel Mac, Apple Silicon Mac, and Linux packages and is suitable for the `main` required-check rule.
5. A `v1.1.0-rc.N` candidate requires N greater than zero and a tagged commit contained in `origin/main`; pull requests, ordinary `main` pushes, and manual runs validate only and never publish.
6. The full Windows EXE, Intel and Apple Silicon Mac DMGs, and Linux x64 AppImage must all succeed in one workflow. Only after exact-inventory, readable MIT license payloads, individual SHA256, aggregate `SHA256SUMS`, SPDX SBOM, four-language notes, and GitHub attestation checks may the workflow atomically create a pre-release. The existing Flameblade Series Gateway synchronizes GitHub Releases hourly; do not add a one-off page-overwrite path.

## 日本語

1. バージョン、4 言語の文書、変更履歴、公式サイトの同期内容を確認します。
2. クリーンなワークツリーで、全体ビルド、CoreHarness、AuditHarness、PackagingAudit、Windows 配布ビルド、ネイティブ Preview パッケージの起動確認、NuGet 脆弱性監査、Gitleaks を実行します。
3. 保護された `main` へ直接 push せず、Pull Request を通じてマージします。
4. 固定名の `Required - All platform packages` 集約チェック、Cross-platform CI、CodeQL、Dependency Review、Security Audit、Secret Defense がすべて成功するまで待ちます。この集約は Windows、Intel Mac、Apple Silicon Mac、Linux の全パッケージを要求し、`main` の required checks に登録できます。
5. `v1.1.0-rc.N` 候補では N が 0 より大きく、タグのコミットが `origin/main` の履歴に含まれていることを必須とします。Pull Request、通常の `main` push、手動実行は検証だけを行い、公開しません。
6. Windows 完全版 EXE、Intel／Apple Silicon Mac DMG、Linux x64 AppImage が同一ワークフローですべて成功し、正確な一覧、各成果物の読みやすい MIT License、個別 SHA256、集約 `SHA256SUMS`、SPDX SBOM、4 言語の説明、GitHub 証明を確認した後にだけ、Pre-release を原子的に作成します。公式サイトは既存の Flameblade Series Gateway が GitHub Releases を 1 時間ごとに同期し、単発のページ上書き処理は追加しません。
