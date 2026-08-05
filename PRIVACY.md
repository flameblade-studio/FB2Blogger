# Privacy / 隱私 / 隐私 / プライバシー

## 繁體中文

### 本機處理與網路連線

FB2Blogger 是本機桌面程式，不會透過炎劍文化工作室的中繼伺服器處理資料。完整 Windows 版只會讀取您主動選擇的 Facebook ZIP，並直接連線至 Google OAuth、Blogger、Google Drive，以及選用的 YouTube API。

### Windows 版保存位置

- Google OAuth 設定與重新整理權杖保存在目前 Windows 使用者的 `%LocalAppData%\FB2Blogger`，並使用 Windows DPAPI 保護。
- 搬移進度與續傳狀態也保存在 `%LocalAppData%\FB2Blogger`。
- 搬移報告保存在目前使用者的「文件\FB2Blogger Reports」。
- 解壓縮內容只會放在作業系統暫存區的 FB2Blogger 專用資料夾；程式會在作業結束時清理，並在下次搬移前重試清理先前殘留項目。

### 跨平台預覽版

目前的 Avalonia 跨平台預覽版只會離線選取、解壓縮與檢查 ZIP，僅保存介面語言，不接受也不保存 OAuth 憑證。每次檢查會使用 `inspect-*` 專用暫存資料夾；若刪除失敗，程式不會靜默忽略，而會顯示錯誤，並在下次啟動與下次檢查前重試。它不會清理其他程式的暫存資料夾。

### 解除安裝提醒

解除安裝程式不一定會刪除上述使用者資料、搬移進度、權杖、報告或因檔案仍被占用而留下的暫存資料。若您不再使用本程式，請在確認不需續傳後自行刪除這些位置；刪除搬移進度後將無法從原進度繼續。

## 简体中文

### 本地处理与网络连接

FB2Blogger 是本地桌面程序，不会通过炎剑文化工作室的中继服务器处理数据。完整 Windows 版只会读取您主动选择的 Facebook ZIP，并直接连接至 Google OAuth、Blogger、Google Drive，以及可选的 YouTube API。

### Windows 版保存位置

- Google OAuth 设置与刷新令牌保存在当前 Windows 用户的 `%LocalAppData%\FB2Blogger`，并使用 Windows DPAPI 保护。
- 迁移进度与续传状态也保存在 `%LocalAppData%\FB2Blogger`。
- 迁移报告保存在当前用户的“文档\FB2Blogger Reports”。
- 解压缩内容只会放在操作系统临时区域的 FB2Blogger 专用文件夹；程序会在操作结束时清理，并在下次迁移前重试清理先前残留项目。

### 跨平台预览版

当前的 Avalonia 跨平台预览版只会离线选择、解压缩与检查 ZIP，仅保存界面语言，不接受也不保存 OAuth 凭据。每次检查会使用 `inspect-*` 专用临时文件夹；若删除失败，程序不会静默忽略，而会显示错误，并在下次启动与下次检查前重试。它不会清理其他程序的临时文件夹。

### 卸载提醒

卸载程序不一定会删除上述用户数据、迁移进度、令牌、报告或因文件仍被占用而留下的临时数据。若您不再使用本程序，请在确认不需要续传后自行删除这些位置；删除迁移进度后将无法从原进度继续。

## English

### Local processing and network connections

FB2Blogger is a local desktop application and does not process data through a Flameblade Studio relay service. The full Windows edition reads only the Facebook ZIP you explicitly select and connects directly to Google OAuth, Blogger, Google Drive, and the optional YouTube API.

### Windows storage locations

- Google OAuth settings and refresh tokens are stored under the current Windows user's `%LocalAppData%\FB2Blogger` and protected with Windows DPAPI.
- Migration progress and resumable state are also stored in `%LocalAppData%\FB2Blogger`.
- Migration reports are stored in the current user's `Documents\FB2Blogger Reports` folder.
- Extracted content is placed only in an FB2Blogger-specific folder under the operating system's temporary area. Cleanup is attempted when an operation ends and retried before the next migration.

### Cross-platform preview

The current Avalonia cross-platform preview only selects, extracts, and inspects a ZIP offline. It stores only the interface language and neither accepts nor stores OAuth credentials. Each inspection uses a dedicated `inspect-*` temporary folder. A deletion failure is never silently ignored: the error is shown and cleanup is retried at the next launch and before the next inspection. Temporary folders belonging to other applications are not touched.

### Uninstall reminder

Uninstalling the application may not remove the user data, migration progress, tokens, reports, or temporary data that remained because files were still in use. If you no longer use FB2Blogger, delete those locations manually after confirming that you do not need to resume a migration. Removing migration state makes resuming from the previous position impossible.

## 日本語

### ローカル処理とネットワーク接続

FB2Blogger はローカルで動作するデスクトップアプリであり、炎剣文化工作室の中継サーバーを介してデータを処理しません。完全版の Windows アプリは、利用者が明示的に選択した Facebook ZIP のみを読み取り、Google OAuth、Blogger、Google Drive、および任意で使用する YouTube API に直接接続します。

### Windows 版の保存場所

- Google OAuth の設定と更新トークンは、現在の Windows ユーザーの `%LocalAppData%\FB2Blogger` に保存され、Windows DPAPI で保護されます。
- 移行の進捗と再開用の状態も `%LocalAppData%\FB2Blogger` に保存されます。
- 移行レポートは、現在のユーザーの `Documents\FB2Blogger Reports` フォルダーに保存されます。
- 展開した内容は、OS の一時領域にある FB2Blogger 専用フォルダーだけに保存されます。処理終了時に削除を試み、残った項目は次回の移行前に再度削除します。

### クロスプラットフォーム・プレビュー版

現在の Avalonia クロスプラットフォーム・プレビュー版は、ZIP の選択、展開、オフライン検査だけを行います。保存するのは表示言語のみで、OAuth 認証情報を受け取ったり保存したりしません。各検査では専用の `inspect-*` 一時フォルダーを使用します。削除に失敗した場合は黙って無視せず、エラーを表示し、次回起動時および次回検査前に削除を再試行します。他のアプリの一時フォルダーには触れません。

### アンインストール時の注意

アンインストールしても、上記のユーザーデータ、移行の進捗、トークン、レポート、またはファイル使用中のため残った一時データが削除されない場合があります。FB2Blogger を今後使用しない場合は、移行を再開する必要がないことを確認してから、これらの保存場所を手動で削除してください。移行状態を削除すると、以前の位置から再開できなくなります。
