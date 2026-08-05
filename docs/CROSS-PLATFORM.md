# Cross-platform foundation / 跨平台基礎 / 跨平台基础 / クロスプラットフォーム基盤

## 繁體中文

### 目前能做什麼

| 元件 | Windows | macOS | Linux |
|---|---|---|---|
| 共用解析核心 | CI 建置與自動測試 | CI 建置與自動測試 | CI 建置與自動測試 |
| Avalonia 預覽殼 | 可建置；可離線檢查 ZIP | 可建置；可離線檢查 ZIP | 可建置；可離線檢查 ZIP |
| 完整 Google 授權、Drive／YouTube 上傳、Blogger 發布 | 既有 WinForms 版完整保留 | 尚未接通 | 尚未接通 |
| 本機憑證保護 | Windows DPAPI，沿用原資料位置 | 規劃接 macOS Keychain | 規劃接 Secret Service／系統金鑰圈 |

這一階段是「能編譯、能測試、能安全檢查 Facebook ZIP」的跨平台基礎，不是 macOS／Linux 正式成品。專案擁有者目前只有 Windows 電腦，因此 macOS／Linux 的 CI 通過只代表雲端建置與自動測試成功，不能代替真實硬體、桌面環境、檔案選擇器與 OAuth 瀏覽器流程的驗證。

### 為什麼保留兩個桌面入口

- `FB2Blogger`：完整 Windows WinForms 版。原有功能、LocalAppData 位置與 DPAPI 保護都不變。
- `FB2Blogger.Desktop`：Avalonia 相對佈局預覽殼，共用同一套四語目錄與解析核心。現在只做離線 ZIP 檢查，避免在安全儲存尚未完成前把 Token 寫成明文。
- `FB2Blogger.Core`：不依賴 WinForms 的模型、Facebook JSON 解析、安全解壓、文章內容組合、搬移進度與平台資料路徑。

### 下一階段守門條件

1. macOS 使用 Keychain、Linux 使用 Secret Service 或相容系統金鑰圈；若安全儲存不可用就停止設定，不得退回明文。
2. Google OAuth 回呼、預設瀏覽器開啟、Drive／YouTube／Blogger 實際帳號流程需在各平台真人驗證。
3. Windows 完整回歸測試、三平台核心測試與三平台 Avalonia 建置全部通過後，才可建立候選版安裝檔。
4. macOS 未簽名套件與 Linux 發行格式必須在 Release 說明中清楚揭露限制。

## 简体中文

当前的共享核心会在 Windows、macOS、Linux 的 CI 中构建并执行自动测试；Avalonia 预览界面也会在三平台构建，并可离线选择、解压和检查 Facebook ZIP。完整的 Google 授权、Drive／YouTube 上传与 Blogger 发布仍只在既有 Windows WinForms 版提供。macOS 将使用 Keychain，Linux 将使用 Secret Service 或系统密钥环；安全存储不可用时不得降级为明文。CI 通过不等于真实 Mac 或 Linux 设备验证，正式发布前仍需要社区工程师提供真实环境测试证据。

## English

The shared core is built and automatically tested on Windows, macOS, and Linux. The Avalonia preview shell also builds on all three platforms and can select, safely extract, and inspect a Facebook ZIP offline. Full Google authorization, Drive/YouTube upload, and Blogger publishing remain available only in the existing Windows WinForms edition. The next stage must use Keychain on macOS and Secret Service or an equivalent system keyring on Linux; it must never fall back to plaintext secrets. Passing CI is not evidence of real Mac or Linux hardware validation, so release readiness still requires documented testing by contributors on those systems.

## 日本語

共通コアは Windows・macOS・Linux の CI でビルドと自動テストを行います。Avalonia のプレビュー画面も 3 OS でビルドし、Facebook ZIP の選択、安全な展開、オフライン解析まで確認できます。Google 認証、Drive／YouTube へのアップロード、Blogger 公開の完全な機能は、現時点では従来の Windows WinForms 版のみです。次の段階では macOS は Keychain、Linux は Secret Service または同等のシステムキーチェーンを使用し、平文保存へは絶対にフォールバックしません。CI 成功は実機検証ではないため、正式公開前に各 OS の協力者による実環境テスト記録が必要です。
