using System.Globalization;

namespace FB2Blogger;

internal sealed record LanguageOption(string Code, string Name)
{
    public override string ToString() => Name;
}

internal static class L
{
    public static readonly LanguageOption[] Supported =
    [
        new("zh-TW", "繁體中文"),
        new("zh-CN", "简体中文"),
        new("en", "English"),
        new("ja", "日本語")
    ];

    static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Texts = BuildTexts();
    public static string Language { get; private set; } = Detect();
    public static string FontName => Language == "ja" ? "Yu Gothic UI" : Language == "en" ? "Segoe UI" : "Microsoft JhengHei UI";

    public static void Configure(string? language)
    {
        Language = Supported.Any(item => item.Code == language) ? language! : Detect();
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(Language);
    }

    public static string T(string key, params object[] args)
    {
        var table = Texts.TryGetValue(Language, out var selected) ? selected : Texts["en"];
        var value = table.TryGetValue(key, out var translated) ? translated : Texts["en"].GetValueOrDefault(key, key);
        return args.Length == 0 ? value : string.Format(CultureInfo.CurrentCulture, value, args);
    }

    internal static IReadOnlyList<string> SupportedCodes => Supported.Select(item => item.Code).ToArray();
    internal static IReadOnlyCollection<string> Keys(string language) => Texts[language].Keys.ToArray();

    static string Detect()
    {
        var name = CultureInfo.CurrentUICulture.Name;
        if (name.StartsWith("ja", StringComparison.OrdinalIgnoreCase)) return "ja";
        if (name.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) || name.Equals("zh-SG", StringComparison.OrdinalIgnoreCase)) return "zh-CN";
        if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return "zh-TW";
        return "en";
    }

    static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> BuildTexts()
    {
        var zhTw = new Dictionary<string, string>
        {
            ["setup_title"] = "FB2Blogger 第一次設定", ["language"] = "介面語言：", ["language_restart"] = "儲存後會以所選語言重新啟動。",
            ["secret_placeholder"] = "Client Secret（若有）", ["draft_default"] = "文章先建立為草稿",
            ["privacy_private"] = "不公開（只有自己）", ["privacy_unlisted"] = "知道網址的人可看", ["privacy_public"] = "公開",
            ["setup_note"] = "只需設定一次\r\n\r\n請在 Google Cloud 建立「Desktop app」OAuth 憑證，並啟用 Blogger API、YouTube Data API、Google Drive API。授權 Token 會以 Windows 帳號加密保存。",
            ["save_connect"] = "儲存並連接 Google", ["cancel"] = "取消", ["missing_client_id"] = "請貼上 Google OAuth Client ID。",
            ["zip_empty"] = "尚未選擇 Facebook ZIP", ["choose_zip"] = "1  選擇 Facebook ZIP", ["start_move"] = "2  開始搬家",
            ["settings"] = "設定", ["pause_move"] = "暫停搬家（可續跑）", ["pause_publish"] = "暫停發布", ["ready"] = "準備就緒",
            ["compose_title_placeholder"] = "文章標題（可留空，自動取內容第一行）", ["compose_body_placeholder"] = "在這裡輸入文章內容；#Hashtag 會自動成為 Blogger Labels",
            ["choose_media"] = "選擇圖片或影片…", ["remove_media"] = "移除選取項目", ["publish_blogger"] = "發布文章到 Blogger",
            ["save_draft"] = "先存成草稿", ["composer_ready"] = "準備就緒。圖片會上傳 Drive；影片會上傳 YouTube。",
            ["tab_move"] = "Facebook 搬家", ["tab_compose"] = "發布新文章", ["compose_heading"] = "撰寫 Blogger 新文章", ["media_heading"] = "圖片與影片（可多選）",
            ["media_dialog_title"] = "選擇圖片或影片", ["media_filter"] = "圖片或影片|*.jpg;*.jpeg;*.png;*.gif;*.webp;*.bmp;*.mp4;*.mov;*.m4v;*.avi;*.mkv;*.webm|所有檔案|*.*",
            ["language_changed"] = "介面語言已更新，程式將重新啟動以完整套用。"
        };
        var zhCn = new Dictionary<string, string>
        {
            ["setup_title"] = "FB2Blogger 初次设置", ["language"] = "界面语言：", ["language_restart"] = "保存后将以所选语言重新启动。",
            ["secret_placeholder"] = "Client Secret（如有）", ["draft_default"] = "文章先创建为草稿",
            ["privacy_private"] = "不公开（仅自己可见）", ["privacy_unlisted"] = "知道链接者可见", ["privacy_public"] = "公开",
            ["setup_note"] = "只需设置一次\r\n\r\n请在 Google Cloud 创建“Desktop app”OAuth 凭据，并启用 Blogger API、YouTube Data API、Google Drive API。授权 Token 会使用 Windows 账户加密保存。",
            ["save_connect"] = "保存并连接 Google", ["cancel"] = "取消", ["missing_client_id"] = "请粘贴 Google OAuth Client ID。",
            ["zip_empty"] = "尚未选择 Facebook ZIP", ["choose_zip"] = "1  选择 Facebook ZIP", ["start_move"] = "2  开始迁移",
            ["settings"] = "设置", ["pause_move"] = "暂停迁移（可继续）", ["pause_publish"] = "暂停发布", ["ready"] = "准备就绪",
            ["compose_title_placeholder"] = "文章标题（可留空，自动采用内容第一行）", ["compose_body_placeholder"] = "在此输入文章内容；#Hashtag 会自动成为 Blogger 标签",
            ["choose_media"] = "选择图片或视频…", ["remove_media"] = "移除所选项目", ["publish_blogger"] = "发布文章到 Blogger",
            ["save_draft"] = "先保存为草稿", ["composer_ready"] = "准备就绪。图片会上传到 Drive；视频会上传到 YouTube。",
            ["tab_move"] = "Facebook 迁移", ["tab_compose"] = "发布新文章", ["compose_heading"] = "撰写 Blogger 新文章", ["media_heading"] = "图片与视频（可多选）",
            ["media_dialog_title"] = "选择图片或视频", ["media_filter"] = "图片或视频|*.jpg;*.jpeg;*.png;*.gif;*.webp;*.bmp;*.mp4;*.mov;*.m4v;*.avi;*.mkv;*.webm|所有文件|*.*",
            ["language_changed"] = "界面语言已更新，程序将重新启动以完整应用。"
        };
        var en = new Dictionary<string, string>
        {
            ["setup_title"] = "FB2Blogger First-time Setup", ["language"] = "Interface language:", ["language_restart"] = "The app will restart in the selected language after saving.",
            ["secret_placeholder"] = "Client Secret (if provided)", ["draft_default"] = "Create posts as drafts first",
            ["privacy_private"] = "Private (only you)", ["privacy_unlisted"] = "Unlisted", ["privacy_public"] = "Public",
            ["setup_note"] = "One-time setup\r\n\r\nCreate a Desktop app OAuth credential in Google Cloud, then enable the Blogger API, YouTube Data API, and Google Drive API. The authorization token is encrypted for your Windows account.",
            ["save_connect"] = "Save and connect to Google", ["cancel"] = "Cancel", ["missing_client_id"] = "Paste your Google OAuth Client ID.",
            ["zip_empty"] = "No Facebook ZIP selected", ["choose_zip"] = "1  Choose Facebook ZIP", ["start_move"] = "2  Start migration",
            ["settings"] = "Settings", ["pause_move"] = "Pause migration (resumable)", ["pause_publish"] = "Pause publishing", ["ready"] = "Ready",
            ["compose_title_placeholder"] = "Post title (optional; uses the first content line)", ["compose_body_placeholder"] = "Write your post here; #hashtags become Blogger labels",
            ["choose_media"] = "Choose images or videos…", ["remove_media"] = "Remove selected", ["publish_blogger"] = "Publish to Blogger",
            ["save_draft"] = "Save as draft first", ["composer_ready"] = "Ready. Images go to Drive; videos go to YouTube.",
            ["tab_move"] = "Facebook Migration", ["tab_compose"] = "New Post", ["compose_heading"] = "Write a new Blogger post", ["media_heading"] = "Images and videos (multiple allowed)",
            ["media_dialog_title"] = "Choose images or videos", ["media_filter"] = "Images or videos|*.jpg;*.jpeg;*.png;*.gif;*.webp;*.bmp;*.mp4;*.mov;*.m4v;*.avi;*.mkv;*.webm|All files|*.*",
            ["language_changed"] = "The interface language was updated. The app will restart to apply it completely."
        };
        var ja = new Dictionary<string, string>
        {
            ["setup_title"] = "FB2Blogger 初回設定", ["language"] = "表示言語：", ["language_restart"] = "保存後、選択した言語で再起動します。",
            ["secret_placeholder"] = "Client Secret（ある場合）", ["draft_default"] = "記事を先に下書きとして作成",
            ["privacy_private"] = "非公開（自分のみ）", ["privacy_unlisted"] = "限定公開", ["privacy_public"] = "公開",
            ["setup_note"] = "初回のみの設定です。\r\n\r\nGoogle Cloud で「Desktop app」の OAuth 認証情報を作成し、Blogger API、YouTube Data API、Google Drive API を有効にしてください。認証トークンは Windows アカウント用に暗号化して保存されます。",
            ["save_connect"] = "保存して Google に接続", ["cancel"] = "キャンセル", ["missing_client_id"] = "Google OAuth Client ID を貼り付けてください。",
            ["zip_empty"] = "Facebook ZIP が選択されていません", ["choose_zip"] = "1  Facebook ZIP を選択", ["start_move"] = "2  移行を開始",
            ["settings"] = "設定", ["pause_move"] = "移行を一時停止（再開可能）", ["pause_publish"] = "公開を一時停止", ["ready"] = "準備完了",
            ["compose_title_placeholder"] = "記事タイトル（空欄時は本文の先頭行を使用）", ["compose_body_placeholder"] = "記事本文を入力；#Hashtag は Blogger のラベルになります",
            ["choose_media"] = "画像または動画を選択…", ["remove_media"] = "選択項目を削除", ["publish_blogger"] = "Blogger に公開",
            ["save_draft"] = "先に下書きとして保存", ["composer_ready"] = "準備完了。画像は Drive、動画は YouTube にアップロードされます。",
            ["tab_move"] = "Facebook 移行", ["tab_compose"] = "新規記事", ["compose_heading"] = "Blogger の新規記事を作成", ["media_heading"] = "画像と動画（複数選択可）",
            ["media_dialog_title"] = "画像または動画を選択", ["media_filter"] = "画像または動画|*.jpg;*.jpeg;*.png;*.gif;*.webp;*.bmp;*.mp4;*.mov;*.m4v;*.avi;*.mkv;*.webm|すべてのファイル|*.*",
            ["language_changed"] = "表示言語を更新しました。完全に適用するため再起動します。"
        };
        AddRuntimeTexts(zhTw, zhCn, en, ja);
        return new Dictionary<string, IReadOnlyDictionary<string, string>> { ["zh-TW"] = zhTw, ["zh-CN"] = zhCn, ["en"] = en, ["ja"] = ja };
    }

    static void AddRuntimeTexts(Dictionary<string, string> tw, Dictionary<string, string> cn, Dictionary<string, string> en, Dictionary<string, string> ja)
    {
        Add("configured_account", "已設定：{0}", "已设置：{0}", "Configured: {0}", "設定済み：{0}");
        Add("google_account", "Google 帳號", "Google 账号", "Google account", "Google アカウント");
        Add("content_required", "請輸入文章內容，或至少選擇一個圖片／影片。", "请输入文章内容，或至少选择一张图片／一个视频。", "Write some content or choose at least one image or video.", "本文を入力するか、画像または動画を1件以上選択してください。");
        Add("new_post_title", "新文章 {0}", "新文章 {0}", "New post {0}", "新規記事 {0}");
        Add("checking_previous_publish", "正在確認上次發布是否其實已經成功…", "正在确认上次发布是否已成功…", "Checking whether the previous publish actually succeeded…", "前回の公開が成功していたか確認しています…");
        Add("duplicate_avoided", "上次發布其實已成功，已避免建立重複文章。", "上次发布实际已成功，已避免创建重复文章。", "The previous publish succeeded; a duplicate post was avoided.", "前回の公開は成功していました。重複記事の作成を防ぎました。");
        Add("post_already_exists", "文章已經存在於 Blogger，程式沒有重複發布。", "文章已存在于 Blogger，程序未重复发布。", "The post already exists in Blogger, so it was not published again.", "記事はすでに Blogger に存在するため、重複公開しませんでした。");
        Add("checking_youtube", "正在檢查 YouTube 影片，避免重複…", "正在检查 YouTube 视频，避免重复…", "Checking YouTube to avoid duplicate uploads…", "重複アップロードを防ぐため YouTube を確認しています…");
        Add("media_not_found", "找不到選取的媒體檔案。", "找不到所选媒体文件。", "The selected media file was not found.", "選択したメディアファイルが見つかりません。");
        Add("reuse_youtube", "沿用既有 YouTube 影片：{0}", "沿用现有 YouTube 视频：{0}", "Reusing an existing YouTube video: {0}", "既存の YouTube 動画を再利用：{0}");
        Add("uploading_video", "正在上傳影片：{0}", "正在上传视频：{0}", "Uploading video: {0}", "動画をアップロード中：{0}");
        Add("uploading_image", "正在智慧壓縮並上傳圖片：{0}", "正在智能压缩并上传图片：{0}", "Optimizing and uploading image: {0}", "画像を最適化してアップロード中：{0}");
        Add("reuse_media", "沿用本次先前已上傳的媒體：{0}", "沿用本次先前已上传的媒体：{0}", "Reusing media uploaded earlier in this run: {0}", "今回すでにアップロードしたメディアを再利用：{0}");
        Add("saving_draft", "正在儲存 Blogger 草稿…", "正在保存 Blogger 草稿…", "Saving Blogger draft…", "Blogger の下書きを保存中…");
        Add("publishing_post", "正在發布 Blogger 文章…", "正在发布 Blogger 文章…", "Publishing to Blogger…", "Blogger に公開中…");
        Add("draft_saved", "文章已存成 Blogger 草稿。", "文章已保存为 Blogger 草稿。", "The post was saved as a Blogger draft.", "記事を Blogger の下書きとして保存しました。");
        Add("post_published", "文章已成功發布到 Blogger。", "文章已成功发布到 Blogger。", "The post was published to Blogger.", "記事を Blogger に公開しました。");
        Add("publish_paused", "發布已暫停；本次已上傳的媒體會暫時保留，按發布可再次嘗試。", "发布已暂停；本次已上传的媒体会暂时保留，可再次点击发布重试。", "Publishing paused. Uploaded media is retained temporarily; choose Publish to try again.", "公開を一時停止しました。アップロード済みメディアは一時保持され、再度公開を選ぶと再試行できます。");
        Add("quota_limit", "Google 配額限制", "Google 配额限制", "Google quota limit", "Google の割り当て制限");
        Add("publish_failed", "發布失敗", "发布失败", "Publishing failed", "公開に失敗");
        Add("publish_failed_detail", "發布失敗：{0}", "发布失败：{0}", "Publishing failed: {0}", "公開に失敗しました：{0}");
        Add("image_reduced", "圖片已縮小：{0} → {1}", "图片已缩小：{0} → {1}", "Image reduced: {0} → {1}", "画像を縮小：{0} → {1}");
        Add("zip_filter", "Facebook ZIP 檔案 (*.zip)|*.zip", "Facebook ZIP 文件 (*.zip)|*.zip", "Facebook ZIP files (*.zip)|*.zip", "Facebook ZIP ファイル (*.zip)|*.zip");
        Add("choose_export_zip", "選擇 Facebook 匯出的 ZIP", "选择 Facebook 导出的 ZIP", "Choose the ZIP exported by Facebook", "Facebook から書き出した ZIP を選択");
        Add("zip_selected", "ZIP 已選好，按「開始搬家」。", "ZIP 已选择，请点击“开始迁移”。", "ZIP selected. Choose Start migration.", "ZIP を選択しました。「移行を開始」を押してください。");
        Add("pausing_safely", "正在安全暫停，請稍候…", "正在安全暂停，请稍候…", "Pausing safely. Please wait…", "安全に一時停止しています。しばらくお待ちください…");
        Add("pause_requested", "已要求暫停；正在保存目前進度…", "已请求暂停；正在保存当前进度…", "Pause requested; saving current progress…", "一時停止を受け付けました。現在の進捗を保存しています…");
        Add("wait_before_close", "正在安全暫停並保存進度。畫面恢復可操作後，再關閉程式。", "正在安全暂停并保存进度。界面恢复可操作后再关闭程序。", "The app is pausing safely and saving progress. Close it only after the window becomes responsive again.", "安全に一時停止して進捗を保存しています。画面が操作可能になってから終了してください。");
        Add("discard_unpublished", "「發布新文章」中還有尚未發布的內容。確定要關閉並捨棄嗎？", "“发布新文章”中仍有未发布内容。确定关闭并舍弃吗？", "The New Post tab contains unpublished content. Close and discard it?", "「新規記事」に未公開の内容があります。破棄して終了しますか？");
        Add("unpublished", "尚未發布", "尚未发布", "Unpublished content", "未公開の内容");
        Add("single_instance", "FB2Blogger 已經在執行中，請回到原本的視窗。", "FB2Blogger 已在运行，请返回原窗口。", "FB2Blogger is already running. Return to the existing window.", "FB2Blogger はすでに実行中です。開いているウィンドウに戻ってください。");
        Add("unexpected_error", "程式遇到未預期問題，已留下錯誤紀錄：\n{0}\n\n{1}", "程序遇到意外问题，已保存错误记录：\n{0}\n\n{1}", "The app encountered an unexpected problem. An error report was saved at:\n{0}\n\n{1}", "予期しない問題が発生しました。エラー記録を保存しました：\n{0}\n\n{1}");
        Add("error_file", "錯誤-{0}.txt", "错误-{0}.txt", "error-{0}.txt", "エラー-{0}.txt");
        Add("error_log_unavailable", "無法寫入錯誤紀錄", "无法写入错误记录", "Unable to write the error report", "エラー記録を書き込めません");
        Add("saved_google_login_expired", "已保存的 Google 登入已失效，需要重新授權一次。", "已保存的 Google 登录已失效，需要重新授权一次。", "The saved Google sign-in has expired and must be authorized again.", "保存済みの Google ログインが無効になりました。もう一度認証してください。");
        Add("google_client_id_missing", "尚未設定 Google OAuth Client ID。", "尚未设置 Google OAuth Client ID。", "The Google OAuth Client ID has not been configured.", "Google OAuth Client ID が設定されていません。");
        Add("google_authorize_in_browser", "第一次使用：請在瀏覽器允許 Google 存取。完成後會自動登入。", "首次使用：请在浏览器中允许 Google 访问。完成后将自动登录。", "First use: allow Google access in the browser. Future sign-ins will be automatic.", "初回のみ、ブラウザーで Google へのアクセスを許可してください。次回からは自動でログインします。");
        Add("oauth_complete_title", "授權完成", "授权完成", "Authorization complete", "認証が完了しました");
        Add("oauth_complete_body", "請關閉此頁並回到 FB2Blogger。", "请关闭此页面并返回 FB2Blogger。", "Close this page and return to FB2Blogger.", "このページを閉じて FB2Blogger に戻ってください。");
        Add("google_authorization_failed", "Google 授權已取消或驗證失敗。", "Google 授权已取消或验证失败。", "Google authorization was cancelled or could not be verified.", "Google の認証がキャンセルされたか、検証に失敗しました。");
        Add("google_refresh_token_missing", "Google 未提供長期登入憑證，請撤銷應用程式權限後重試。", "Google 未提供长期登录凭据，请撤销应用权限后重试。", "Google did not provide a long-term sign-in credential. Revoke the app permission, then try again.", "Google から長期ログイン用の認証情報が返されませんでした。アプリの権限を取り消してから再試行してください。");
        Add("google_sign_in_failed", "Google 登入失敗：{0}", "Google 登录失败：{0}", "Google sign-in failed: {0}", "Google へのログインに失敗しました：{0}");
        Add("google_sign_in_invalid_response", "Google 登入回應無效。", "Google 登录响应无效。", "Google returned an invalid sign-in response.", "Google から無効なログイン応答が返されました。");
        Add("google_access_token_missing", "Google 沒有回傳存取權杖。", "Google 未返回访问令牌。", "Google did not return an access token.", "Google からアクセストークンが返されませんでした。");
        Add("action_read_blogger", "讀取 Blogger", "读取 Blogger", "Read Blogger", "Blogger を読み込み");
        Add("action_upload_image", "上傳圖片", "上传图片", "Upload image", "画像をアップロード");
        Add("drive_image_id_missing", "Google Drive 未回傳圖片 ID。", "Google Drive 未返回图片 ID。", "Google Drive did not return an image ID.", "Google Drive から画像 ID が返されませんでした。");
        Add("action_set_image_permission", "設定圖片顯示權限", "设置图片显示权限", "Set image viewing permission", "画像の表示権限を設定");
        Add("action_check_drive_images", "檢查 Drive 圖片", "检查 Drive 图片", "Check Drive images", "Drive の画像を確認");
        Add("action_update_drive_image", "更新既有 Drive 圖片", "更新现有 Drive 图片", "Update an existing Drive image", "既存の Drive 画像を更新");
        Add("action_start_youtube_upload", "啟動 YouTube 上傳", "启动 YouTube 上传", "Start YouTube upload", "YouTube アップロードを開始");
        Add("youtube_upload_url_missing", "YouTube 未提供上傳網址。", "YouTube 未提供上传地址。", "YouTube did not provide an upload URL.", "YouTube からアップロード URL が返されませんでした。");
        Add("action_upload_youtube_video", "上傳 YouTube 影片", "上传 YouTube 视频", "Upload YouTube video", "YouTube 動画をアップロード");
        Add("youtube_video_id_missing", "YouTube 未回傳影片 ID。", "YouTube 未返回视频 ID。", "YouTube did not return a video ID.", "YouTube から動画 ID が返されませんでした。");
        Add("action_create_blogger_post", "建立 Blogger 文章", "创建 Blogger 文章", "Create Blogger post", "Blogger の記事を作成");
        Add("blogger_post_quota_exhausted", "Blogger 目前的新增文章配額已用完。程式已安全停止；請稍後或明天再選同一個 ZIP，會從未完成處繼續。", "Blogger 当前的新增文章配额已用完。程序已安全停止；请稍后或明天再次选择同一个 ZIP，将从未完成处继续。", "Blogger's current post-creation quota is exhausted. The app stopped safely; choose the same ZIP later or tomorrow to resume.", "Blogger の記事作成割り当てを使い切りました。安全に停止したため、後ほど、または翌日に同じ ZIP を選ぶと未完了部分から再開できます。");
        Add("blogger_rate_limited", "Blogger 暫時限制速度，等待 {0} 秒後自動重試（{1}/{2}）…", "Blogger 暂时限制请求速度，将在 {0} 秒后自动重试（{1}/{2}）…", "Blogger is temporarily rate-limiting requests. Retrying in {0} seconds ({1}/{2})…", "Blogger により一時的に速度制限されています。{0} 秒後に自動再試行します（{1}/{2}）…");
        Add("action_check_blogger_posts", "檢查 Blogger 文章", "检查 Blogger 文章", "Check Blogger posts", "Blogger の記事を確認");
        Add("action_check_youtube_channel", "檢查 YouTube 頻道", "检查 YouTube 频道", "Check YouTube channel", "YouTube チャンネルを確認");
        Add("action_read_youtube_uploads", "讀取 YouTube 上傳紀錄", "读取 YouTube 上传记录", "Read YouTube upload history", "YouTube のアップロード履歴を読み込み");
        Add("action_read_youtube_fingerprints", "讀取 YouTube 影片指紋", "读取 YouTube 视频指纹", "Read YouTube video fingerprints", "YouTube 動画の照合情報を読み込み");
        Add("google_action_quota", "{0}受到 Google 配額限制：{1}", "{0}受到 Google 配额限制：{1}", "{0} hit a Google quota limit: {1}", "{0}で Google の割り当て制限に達しました：{1}");
        Add("action_failed", "{0}失敗：{1}", "{0}失败：{1}", "{0} failed: {1}", "{0}に失敗しました：{1}");
        Add("parser_skip_file", "略過 {0}：{1}", "跳过 {0}：{1}", "Skipped {0}: {1}", "{0} をスキップしました：{1}");
        Add("facebook_post_title", "Facebook 貼文 {0:yyyy-MM-dd HH:mm}", "Facebook 帖子 {0:yyyy-MM-dd HH:mm}", "Facebook post {0:yyyy-MM-dd HH:mm}", "Facebook 投稿 {0:yyyy-MM-dd HH:mm}");
        Add("article_image_alt", "文章圖片", "文章图片", "Post image", "記事の画像");
        Add("facebook_image_alt", "Facebook 圖片", "Facebook 图片", "Facebook image", "Facebook の画像");
        Add("blogger_site_missing", "這個 Google 帳號還沒有 Blogger 網誌，請先在 Blogger 建立一個網誌。", "此 Google 账号尚无 Blogger 博客，请先在 Blogger 创建一个博客。", "This Google account does not have a Blogger blog yet. Create one in Blogger first.", "この Google アカウントには Blogger ブログがありません。先に Blogger でブログを作成してください。");
        Add("choose_blogger_site", "選擇 Blogger 網誌", "选择 Blogger 博客", "Choose a Blogger blog", "Blogger ブログを選択");
        Add("ok", "確定", "确定", "OK", "決定");
        Add("setup_complete", "設定完成：{0}。以後不需再登入。", "设置完成：{0}。以后无需再次登录。", "Setup complete: {0}. You will not need to sign in again.", "設定完了：{0}。次回から再ログインは不要です。");
        Add("setup_failed", "設定失敗", "设置失败", "Setup failed", "設定に失敗");
        Add("setup_failed_detail", "設定失敗：{0}", "设置失败：{0}", "Setup failed: {0}", "設定に失敗しました：{0}");
        Add("choose_zip_first", "請先選擇 Facebook ZIP。", "请先选择 Facebook ZIP。", "Choose a Facebook ZIP first.", "先に Facebook ZIP を選択してください。");
        Add("cleaning_stale_temp", "正在清理上次可能留下的暫存資料…", "正在清理上次可能遗留的临时数据…", "Cleaning temporary data left by a previous run…", "前回の一時データを整理しています…");
        Add("extracting_facebook_zip", "正在解開 Facebook ZIP…", "正在解压 Facebook ZIP…", "Extracting the Facebook ZIP…", "Facebook ZIP を展開しています…");
        Add("finding_export_content", "正在尋找貼文、圖片與影片…", "正在查找帖子、图片与视频…", "Finding posts, images, and videos…", "投稿、画像、動画を検索しています…");
        Add("facebook_posts_missing", "找不到 Facebook 貼文。請從 Facebook 下載「JSON」格式，而不是 HTML 格式。", "找不到 Facebook 帖子。请从 Facebook 下载“JSON”格式，而不是 HTML 格式。", "No Facebook posts were found. Download your Facebook data in JSON format, not HTML.", "Facebook の投稿が見つかりません。HTML ではなく JSON 形式でダウンロードしてください。");
        Add("checking_blogger_existing", "正在核對 Blogger，找出已刪除或已存在的文章…", "正在核对 Blogger，查找已删除或已存在的文章…", "Checking Blogger for deleted or existing posts…", "削除済みまたは既存の記事を Blogger で確認しています…");
        Add("checking_youtube_existing", "正在核對 YouTube，避免影片重複上傳…", "正在核对 YouTube，避免视频重复上传…", "Checking YouTube to avoid duplicate video uploads…", "動画の重複アップロードを防ぐため YouTube を確認しています…");
        Add("skip_existing_post", "略過 Blogger 中仍存在的文章 {0}/{1}", "跳过 Blogger 中仍存在的文章 {0}/{1}", "Skipping post that still exists in Blogger {0}/{1}", "Blogger に存在する記事をスキップ {0}/{1}");
        Add("migrating_post", "正在搬第 {0}/{1} 篇：{2}", "正在迁移第 {0}/{1} 篇：{2}", "Migrating post {0}/{1}: {2}", "記事を移行中 {0}/{1}：{2}");
        Add("media_path_missing", "找不到媒體：{0}", "找不到媒体：{0}", "Media not found: {0}", "メディアが見つかりません：{0}");
        Add("reuse_previous_youtube", "沿用先前已上傳的 YouTube 影片。", "沿用先前已上传的 YouTube 视频。", "Reusing a previously uploaded YouTube video.", "以前アップロードした YouTube 動画を再利用します。");
        Add("reuse_previous_image", "沿用先前已上傳的圖片。", "沿用先前已上传的图片。", "Reusing a previously uploaded image.", "以前アップロードした画像を再利用します。");
        Add("found_existing_youtube", "找到先前已上傳的 YouTube 影片，直接沿用。", "找到先前已上传的 YouTube 视频，将直接沿用。", "Found a previously uploaded YouTube video; reusing it.", "以前アップロードした YouTube 動画が見つかったため再利用します。");
        Add("upload_video_to_youtube", "上傳影片到 YouTube…", "正在将视频上传到 YouTube…", "Uploading video to YouTube…", "動画を YouTube にアップロード中…");
        Add("optimize_upload_image", "智慧壓縮並上傳圖片…", "智能压缩并上传图片…", "Optimizing and uploading image…", "画像を最適化してアップロード中…");
        Add("post_failed_continue", "此篇失敗，繼續下一篇：{0}", "此篇失败，继续下一篇：{0}", "This post failed; continuing with the next: {0}", "この記事は失敗しました。次へ進みます：{0}");
        Add("completed_progress", "已完成 {0}/{1}", "已完成 {0}/{1}", "Completed {0}/{1}", "完了 {0}/{1}");
        Add("migration_stopped_resume", "已停止；下次選同一個 ZIP 會從未完成處繼續。", "已停止；下次选择同一个 ZIP 会从未完成处继续。", "Stopped. Choose the same ZIP next time to resume.", "停止しました。次回同じ ZIP を選ぶと未完了部分から再開します。");
        Add("google_quota_paused", "Google 配額暫停", "Google 配额暂停", "Paused for Google quota", "Google の割り当てにより一時停止");
        Add("migration_failed", "搬家失敗：{0}", "迁移失败：{0}", "Migration failed: {0}", "移行に失敗しました：{0}");
        Add("stopped", "已停止", "已停止", "Stopped", "停止");
        Add("completed", "完成", "完成", "Completed", "完了");
        Add("migration_completed", "搬家完成", "迁移完成", "Migration complete", "移行完了");
        Add("migration_report_text", "FB2Blogger 搬家報告\r\n時間：{0:g} - {1:g}\r\n狀態：{2}\r\n總文章：{3}\r\n成功：{4}\r\n已完成略過：{5}\r\n失敗：{6}\r\n圖片：{7}\r\n影片：{8}\r\n", "FB2Blogger 迁移报告\r\n时间：{0:g} - {1:g}\r\n状态：{2}\r\n文章总数：{3}\r\n成功：{4}\r\n已完成并跳过：{5}\r\n失败：{6}\r\n图片：{7}\r\n视频：{8}\r\n", "FB2Blogger migration report\r\nTime: {0:g} - {1:g}\r\nStatus: {2}\r\nTotal posts: {3}\r\nSucceeded: {4}\r\nCompleted and skipped: {5}\r\nFailed: {6}\r\nImages: {7}\r\nVideos: {8}\r\n", "FB2Blogger 移行レポート\r\n時間：{0:g} - {1:g}\r\n状態：{2}\r\n記事総数：{3}\r\n成功：{4}\r\n完了済みのためスキップ：{5}\r\n失敗：{6}\r\n画像：{7}\r\n動画：{8}\r\n");
        Add("failure_details", "\r\n失敗明細：\r\n", "\r\n失败明细：\r\n", "\r\nFailure details:\r\n", "\r\n失敗の詳細：\r\n");
        Add("report_write_failed", "報告未能寫入，但搬家進度已安全保存。", "报告无法写入，但迁移进度已安全保存。", "The report could not be written, but migration progress was saved safely.", "レポートを書き込めませんでしたが、移行の進捗は安全に保存されています。");
        Add("report_file_name", "搬家報告-{0:yyyyMMdd-HHmmss}.txt", "迁移报告-{0:yyyyMMdd-HHmmss}.txt", "migration-report-{0:yyyyMMdd-HHmmss}.txt", "移行レポート-{0:yyyyMMdd-HHmmss}.txt");
        Add("report_saved", "報告已保存到「文件\\FB2Blogger Reports」。", "报告已保存到“文档\\FB2Blogger Reports”。", "The report was saved in Documents\\FB2Blogger Reports.", "レポートを「ドキュメント\\FB2Blogger Reports」に保存しました。");
        Add("migration_stopped_summary", "搬家已停止。{0}", "迁移已停止。{0}", "Migration stopped. {0}", "移行を停止しました。{0}");
        Add("migration_completed_summary", "搬家完成：成功 {0}，略過 {1}，失敗 {2}。{3}", "迁移完成：成功 {0}，跳过 {1}，失败 {2}。{3}", "Migration complete: {0} succeeded, {1} skipped, {2} failed. {3}", "移行完了：成功 {0}、スキップ {1}、失敗 {2}。{3}");
        Add("migration_report_dialog", "{0}\n\n成功：{1}\n已完成略過：{2}\n失敗：{3}\n圖片：{4}\n影片：{5}\n\n{6}", "{0}\n\n成功：{1}\n已完成并跳过：{2}\n失败：{3}\n图片：{4}\n视频：{5}\n\n{6}", "{0}\n\nSucceeded: {1}\nCompleted and skipped: {2}\nFailed: {3}\nImages: {4}\nVideos: {5}\n\n{6}", "{0}\n\n成功：{1}\n完了済みのためスキップ：{2}\n失敗：{3}\n画像：{4}\n動画：{5}\n\n{6}");
        Add("migration_report_title", "FB2Blogger 搬家報告", "FB2Blogger 迁移报告", "FB2Blogger migration report", "FB2Blogger 移行レポート");
        Add("checking_previous_images", "正在檢查先前上傳的圖片，符合條件時原位壓縮…", "正在检查先前上传的图片，符合条件时将原位压缩…", "Checking previously uploaded images and optimizing eligible files in place…", "以前アップロードした画像を確認し、条件に合う画像をそのまま最適化しています…");
        Add("optimizing_existing_image", "壓縮既有圖片：{0}（{1} → {2}）", "压缩现有图片：{0}（{1} → {2}）", "Optimizing existing image: {0} ({1} → {2})", "既存の画像を最適化：{0}（{1} → {2}）");
        Add("zip_too_many_entries", "ZIP 內檔案數異常過多，為保護電腦已停止解壓縮。", "ZIP 内文件数量异常过多，为保护电脑已停止解压。", "The ZIP contains an unusually large number of files. Extraction stopped to protect the computer.", "ZIP 内のファイル数が異常に多いため、パソコンを保護するため展開を停止しました。");
        Add("zip_file_name_too_long", "ZIP 包含異常過長的檔名。", "ZIP 包含异常过长的文件名。", "The ZIP contains an unusually long file name.", "ZIP に異常に長いファイル名が含まれています。");
        Add("zip_size_invalid", "ZIP 宣告的容量異常。", "ZIP 声明的容量异常。", "The ZIP declares an invalid size.", "ZIP に記録された容量が不正です。");
        Add("zip_ratio_invalid", "ZIP 壓縮比例異常，可能是會塞滿硬碟的惡意壓縮檔。", "ZIP 压缩比异常，可能是会占满硬盘的恶意压缩文件。", "The ZIP has an abnormal compression ratio and may be a malicious archive that could fill the disk.", "ZIP の圧縮率が異常です。ディスクを埋め尽くす悪意ある圧縮ファイルの可能性があります。");
        Add("disk_space_insufficient", "系統碟空間不足。解壓縮約需 {0}，目前可安全使用約 {1}。", "系统盘空间不足。解压约需 {0}，当前可安全使用约 {1}。", "The system drive does not have enough space. Extraction needs about {0}; approximately {1} is safely available.", "システムドライブの空き容量が不足しています。展開には約 {0} 必要で、安全に使用できる容量は約 {1} です。");
        Add("zip_unsafe_path", "ZIP 包含不安全的路徑。", "ZIP 包含不安全的路径。", "The ZIP contains an unsafe path.", "ZIP に安全でないパスが含まれています。");

        void Add(string key, string zhTw, string zhCn, string english, string japanese)
        {
            tw[key] = zhTw; cn[key] = zhCn; en[key] = english; ja[key] = japanese;
        }
    }
}
