# Native preview packages / 原生預覽封裝 / 原生预览软件包 / ネイティブ・プレビュー・パッケージ

## 繁體中文

### 下載內容與功能邊界

- macOS 提供 Intel x64 與 Apple Silicon arm64 兩個未使用 Apple Developer ID 簽章、未公證的 Preview `.dmg`，每個映像都內含真正的 `.app`。
- Linux 提供 x64 Preview `.AppImage`。它是 AppImage 規格的可執行檔，不是改副檔名的 ZIP。
- 兩種預覽版都只提供離線選擇、安全解壓與解析本人從 Facebook 官方取得的 ZIP。Google OAuth、Drive／YouTube 上傳與 Blogger 發布仍只存在於 Windows 完整版。
- 預覽版不會要求或保存 OAuth 憑證；請勿把 Client Secret 或 Token 輸入預覽版。

### 自動驗證與可追溯性

GitHub Actions 分別在原生 `macos-15-intel`、Apple Silicon `macos-15` 與 `ubuntu-24.04` runner 建置封裝，掛載或解開成品，再直接執行封裝內程式的啟動檢查。每份成品隨附 SHA256 與 SPDX SBOM。可信任的 `main`、版本標籤與手動工作流程另由 GitHub 簽發 SLSA 來源證明及 SBOM 證明；Pull Request 驗證產物不簽發證明。

```bash
# macOS
shasum -a 256 -c FB2Blogger-*.dmg.sha256

# Linux
sha256sum -c FB2Blogger-*.AppImage.sha256

# 可信任工作流程的 GitHub 證明
gh attestation verify FB2Blogger-*.dmg --repo hitoshic1982/FB2Blogger
gh attestation verify FB2Blogger-*.AppImage --repo hitoshic1982/FB2Blogger
```

### 開啟方式與實機狀態

macOS 使用者請選擇符合處理器的 DMG，將 App 拖入「應用程式」，再按住 Control 點選 App 並選擇「打開」。因為尚未購買 Apple Developer ID，也未經 Apple 公證，系統會顯示安全提醒；請核對 SHA256，不要關閉整部電腦的安全防護。Linux 使用者核對雜湊後執行 `chmod +x 檔名.AppImage`；缺少 FUSE 時可用 `APPIMAGE_EXTRACT_AND_RUN=1 ./檔名.AppImage`。

原生 runner 的啟動檢查證明封裝可建置、可掛載／解開並能啟動，但不等於專案擁有者已在真實 Mac 或 Linux 桌面環境驗證檔案選擇器、字型、視窗管理與長時間操作。請回報作業系統版本、CPU、桌面環境、測試步驟與結果；在累積真實證據前，這些成品一律維持 **Preview**。

## 简体中文

### 下载内容与功能边界

- macOS 提供 Intel x64 与 Apple Silicon arm64 两个未使用 Apple Developer ID 签名、未公证的 Preview `.dmg`，每个映像都包含真正的 `.app`。
- Linux 提供 x64 Preview `.AppImage`，它是符合 AppImage 规范的可执行文件，不是修改扩展名的 ZIP。
- 两种预览版都只支持离线选择、安全解压和解析本人从 Facebook 官方取得的 ZIP。Google OAuth、Drive／YouTube 上传与 Blogger 发布仍仅由 Windows 完整版提供。
- 预览版不会要求或保存 OAuth 凭据；请勿把 Client Secret 或 Token 输入预览版。

### 自动验证与可追溯性

GitHub Actions 分别在原生 `macos-15-intel`、Apple Silicon `macos-15` 与 `ubuntu-24.04` runner 构建软件包，挂载或解开成品，并直接执行软件包内程序的启动检查。每份成品附带 SHA256 与 SPDX SBOM。可信的 `main`、版本标签和手动工作流还会由 GitHub 签发 SLSA 来源证明及 SBOM 证明；Pull Request 验证产物不会签发证明。

macOS 请先核对 SHA256，选择与处理器一致的 DMG，将 App 拖入“应用程序”，再按住 Control 点击 App 并选择“打开”。由于没有 Apple Developer ID 签名和公证，系统会显示安全提醒；请勿关闭整台电脑的安全防护。Linux 核对哈希后运行 `chmod +x 文件名.AppImage`；缺少 FUSE 时可使用 `APPIMAGE_EXTRACT_AND_RUN=1 ./文件名.AppImage`。

原生 runner 的启动检查不等于真实 Mac 或 Linux 桌面实机验证。请在反馈中注明系统版本、CPU、桌面环境、测试步骤和结果；在取得足够真实证据之前，这些软件包始终标记为 **Preview**。

## English

### Downloads and functional boundary

- macOS receives separate Intel x64 and Apple Silicon arm64 Preview `.dmg` files. Each disk image contains a real `.app`; it is not signed with an Apple Developer ID and is not notarized.
- Linux receives an x64 Preview `.AppImage` that follows the AppImage format. It is not a ZIP with a renamed extension.
- Both previews only select, safely extract, and parse an official Facebook export ZIP offline. Google OAuth, Drive/YouTube upload, and Blogger publishing remain exclusive to the full Windows edition.
- The previews neither request nor store OAuth credentials. Never enter a Client Secret or token into a preview build.

### Automated evidence and traceability

GitHub Actions builds on native `macos-15-intel`, Apple Silicon `macos-15`, and `ubuntu-24.04` runners. It mounts or extracts the finished package and directly invokes the packaged executable's launch smoke test. Every package includes a SHA256 file and an SPDX SBOM. Trusted `main`, version-tag, and manual runs also receive GitHub-signed SLSA provenance and SBOM attestations; pull-request validation artifacts are deliberately not attested.

On macOS, verify SHA256, choose the DMG matching the processor, drag the app to Applications, then Control-click the app and choose Open. Gatekeeper warns because there is no Apple Developer ID signature or notarization; do not disable system-wide security controls. On Linux, verify the hash and run `chmod +x filename.AppImage`. If FUSE is unavailable, use `APPIMAGE_EXTRACT_AND_RUN=1 ./filename.AppImage`.

A native-runner launch check proves that the package can be built, mounted or extracted, and started. It is not evidence that the project owner tested file pickers, fonts, window management, or long-running use on real Mac or Linux hardware. Reports should include the OS version, CPU, desktop environment, steps, and outcome. These packages remain **Preview** until enough real-world evidence exists.

## 日本語

### 配布内容と機能の境界

- macOS では Intel x64 と Apple Silicon arm64 向けに、Apple Developer ID 署名および公証を行っていない Preview `.dmg` を個別に作成します。各ディスクイメージには実際の `.app` が入ります。
- Linux では x64 Preview `.AppImage` を作成します。拡張子だけを変更した ZIP ではなく、AppImage 形式の実行ファイルです。
- どちらのプレビューも、Facebook 公式エクスポート ZIP の選択、安全な展開、オフライン解析だけに対応します。Google OAuth、Drive／YouTube へのアップロード、Blogger 公開は引き続き Windows 完全版のみです。
- プレビュー版は OAuth 認証情報を要求も保存もしません。Client Secret やトークンを入力しないでください。

### 自動検証と追跡可能性

GitHub Actions は、ネイティブの `macos-15-intel`、Apple Silicon `macos-15`、`ubuntu-24.04` runner でパッケージを作成します。完成品をマウントまたは展開し、パッケージ内の実行ファイルを直接呼び出して起動確認を行います。各パッケージには SHA256 と SPDX SBOM を添付します。信頼できる `main`、バージョンタグ、手動実行では GitHub 署名付き SLSA 来歴証明と SBOM 証明も作成し、Pull Request の検証成果物には意図的に証明を発行しません。

macOS では SHA256 を確認し、CPU に合う DMG を選び、App を「アプリケーション」へ移動してください。その後 Control キーを押しながら App をクリックし、「開く」を選びます。Apple Developer ID 署名と公証がないため警告が出ますが、OS 全体の安全機能は無効にしないでください。Linux ではハッシュ確認後に `chmod +x ファイル名.AppImage` を実行します。FUSE がない環境では `APPIMAGE_EXTRACT_AND_RUN=1 ./ファイル名.AppImage` を使用できます。

ネイティブ runner の起動確認は、ビルド、マウント／展開、起動が可能であることを示しますが、プロジェクト所有者が実機 Mac／Linux でファイル選択、フォント、ウィンドウ管理、長時間動作を検証した証拠ではありません。報告には OS、CPU、デスクトップ環境、手順、結果を記載してください。十分な実環境証拠が集まるまで、配布物は **Preview** のままです。
