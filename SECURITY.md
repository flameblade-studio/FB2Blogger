# Security / 安全性 / 安全 / セキュリティ

## 繁體中文

請勿在公開 Issue 貼上 OAuth Client Secret、Token、Facebook ZIP、私人文章或個人資料。安全漏洞請寄至 `master@flamebladestudio.com.tw`，主旨註明 `FB2Blogger Security`，並附上版本、重現步驟與影響。收到後會先確認、評估修補並在適當時機公開說明。

僅 GitHub Releases 中由本儲存庫發布、且 SHA256 相符的檔案視為官方成品。四平台候選版各附 SPDX SBOM；只有符合精確 RC 標籤、標籤提交位於 `main` 歷史中且所有平台驗證成功時，才會產生 GitHub 來源與 SBOM 證明並發布。PR、一般 `main` push 與手動驗證產物不簽發證明，也不是官方 Release。Windows 完整版以 DPAPI 保護本機 OAuth 資料；跨平台預覽殼不接收 OAuth 憑證。未來 macOS／Linux 接上完整功能時，必須使用系統金鑰圈，安全儲存不可用時不得退回明文。使用者仍須保護系統帳號與 Google Cloud 設定。

## 简体中文

请勿在公开 Issue 中粘贴 OAuth Client Secret、Token、Facebook ZIP、私人文章或个人信息。安全漏洞请发送至 `master@flamebladestudio.com.tw`，邮件主题注明 `FB2Blogger Security`，并附上版本、复现步骤和影响。仅 GitHub Releases 中由本仓库发布且 SHA256 一致的文件属于官方构建。四平台候选版分别附带 SPDX SBOM；只有符合严格 RC 标签、标签提交属于 `main` 历史且全部平台验证成功时，才会生成 GitHub 来源与 SBOM 证明并发布。PR、普通 `main` push 与手动验证产物不会签发证明，也不是官方 Release。跨平台预览界面不接收 OAuth 凭证；未来 macOS／Linux 完整版必须使用系统密钥环，不得降级为明文保存。

## English

Never post OAuth secrets, tokens, Facebook archives, private posts, or personal data in a public issue. Report vulnerabilities to `master@flamebladestudio.com.tw` with the subject `FB2Blogger Security`, including the affected version, reproduction steps, and impact. Only assets published by this repository under GitHub Releases and matching the listed SHA256 are official builds. Every four-platform RC package has an SPDX SBOM. GitHub provenance and SBOM attestations are issued only when an exact RC tag belongs to `main` history and all platform gates pass. Pull requests, ordinary `main` pushes, and manual validation runs remain unattested and are not official releases. The cross-platform preview does not accept OAuth credentials. A future full macOS/Linux edition must use the system keyring and must never fall back to plaintext storage.

## 日本語

OAuth の秘密情報、トークン、Facebook ZIP、非公開記事、個人情報を公開 Issue に貼らないでください。脆弱性は件名を `FB2Blogger Security` とし、対象バージョン、再現手順、影響を添えて `master@flamebladestudio.com.tw` へ報告してください。GitHub Releases で本リポジトリが公開し、SHA256 が一致するファイルだけが公式ビルドです。4 プラットフォームの RC にはそれぞれ SPDX SBOM を添付します。正確な RC タグが `main` の履歴に含まれ、全プラットフォームの検証が成功した場合だけ、GitHub の来歴証明と SBOM 証明を作成して公開します。Pull Request、通常の `main` push、手動検証の成果物には証明を発行せず、公式 Release とも扱いません。クロスプラットフォーム・プレビューは OAuth 情報を受け取りません。将来の macOS／Linux 完全版はシステムキーチェーンを使用し、平文保存へフォールバックしてはいけません。

