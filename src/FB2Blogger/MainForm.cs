using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace FB2Blogger;

internal sealed class MainForm : Form
{
    readonly TextBox zipPath = new() { ReadOnly = true, PlaceholderText = L.T("zip_empty"), Dock = DockStyle.Fill };
    readonly Button choose = new() { Text = L.T("choose_zip"), Height = 45, Dock = DockStyle.Fill };
    readonly Button start = new() { Text = L.T("start_move"), Height = 52, Dock = DockStyle.Fill, Enabled = false };
    readonly Button settingsButton = new() { Text = L.T("settings"), AutoSize = true };
    readonly Button stop = new() { Text = L.T("pause_move"), Height = 52, Dock = DockStyle.Fill, Enabled = false, BackColor = Color.IndianRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
    readonly Button stopCompose = new() { Text = L.T("pause_publish"), Height = 52, Dock = DockStyle.Fill, Enabled = false, BackColor = Color.IndianRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
    readonly ProgressBar progress = new() { Dock = DockStyle.Fill };
    readonly Label status = new() { Text = L.T("ready"), AutoSize = true };
    readonly TextBox log = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    readonly TextBox composeTitle = new() { PlaceholderText = L.T("compose_title_placeholder"), Dock = DockStyle.Fill };
    readonly TextBox composeBody = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true, PlaceholderText = L.T("compose_body_placeholder"), Dock = DockStyle.Fill };
    readonly ListBox composeMedia = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true };
    readonly Button addMedia = new() { Text = L.T("choose_media"), AutoSize = true };
    readonly Button removeMedia = new() { Text = L.T("remove_media"), AutoSize = true };
    readonly Button publishArticle = new() { Text = L.T("publish_blogger"), Height = 52, Dock = DockStyle.Fill };
    readonly CheckBox composeDraft = new() { Text = L.T("save_draft"), AutoSize = true };
    readonly Label composeStatus = new() { Text = L.T("composer_ready"), ForeColor = Color.DimGray, AutoSize = true };
    readonly Dictionary<string, HostedMedia> composeMediaCache = new(StringComparer.OrdinalIgnoreCase);
    string composePostKey = "";
    readonly AppSettings settings;
    CancellationTokenSource? cts;

    public MainForm(AppSettings settings)
    {
        this.settings = settings;
        Text = "FB2Blogger"; Width = 900; Height = 650; MinimumSize = new(760, 560); StartPosition = FormStartPosition.CenterScreen; Font = new(L.FontName, 10);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new(16), ColumnCount = 1, RowCount = 8 };
        layout.RowStyles.Add(new(SizeType.Absolute, 58)); layout.RowStyles.Add(new(SizeType.Absolute, 55)); layout.RowStyles.Add(new(SizeType.Absolute, 44)); layout.RowStyles.Add(new(SizeType.Absolute, 64)); layout.RowStyles.Add(new(SizeType.Absolute, 35)); layout.RowStyles.Add(new(SizeType.Absolute, 34)); layout.RowStyles.Add(new(SizeType.Percent, 100)); layout.RowStyles.Add(new(SizeType.Absolute, 42));
        layout.Controls.Add(new Label { Text = "FB2Blogger", Font = new("Microsoft JhengHei UI", 22, FontStyle.Bold), AutoSize = true });
        layout.Controls.Add(choose); layout.Controls.Add(zipPath);
        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 }; actions.ColumnStyles.Add(new(SizeType.Percent, 65)); actions.ColumnStyles.Add(new(SizeType.Percent, 35)); actions.Controls.Add(start, 0, 0); actions.Controls.Add(stop, 1, 0); layout.Controls.Add(actions);
        layout.Controls.Add(progress); layout.Controls.Add(status); layout.Controls.Add(log);
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft }; bottom.Controls.Add(settingsButton); layout.Controls.Add(bottom);
        var migrationTab = new TabPage(L.T("tab_move")) { Padding = new(4) }; migrationTab.Controls.Add(layout);
        var composeTab = new TabPage(L.T("tab_compose")) { Padding = new(4) }; composeTab.Controls.Add(BuildComposer());
        var tabs = new TabControl { Dock = DockStyle.Fill, Font = Font }; tabs.TabPages.Add(migrationTab); tabs.TabPages.Add(composeTab); Controls.Add(tabs);
        choose.Click += ChooseZip; start.Click += StartMigration; settingsButton.Click += Configure; stop.Click += RequestPause; stopCompose.Click += RequestPause; FormClosing += HandleFormClosing;
        addMedia.Click += AddComposerMedia; removeMedia.Click += (_, _) => { while (composeMedia.SelectedIndices.Count > 0) composeMedia.Items.RemoveAt(composeMedia.SelectedIndices[0]); composePostKey = ""; };
        publishArticle.Click += PublishArticle;
        composeTitle.TextChanged += (_, _) => { if (cts is null) composePostKey = ""; };
        composeBody.TextChanged += (_, _) => { if (cts is null) composePostKey = ""; };
        Shown += async (_, _) => { if (string.IsNullOrWhiteSpace(settings.ClientId)) await ConfigureFirstRunAsync(); else Say(L.T("configured_account", string.IsNullOrEmpty(settings.BlogName) ? L.T("google_account") : settings.BlogName)); };
    }

    Control BuildComposer()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new(16), ColumnCount = 1, RowCount = 8 };
        panel.RowStyles.Add(new(SizeType.Absolute, 42)); panel.RowStyles.Add(new(SizeType.Absolute, 45)); panel.RowStyles.Add(new(SizeType.Percent, 55)); panel.RowStyles.Add(new(SizeType.Absolute, 34)); panel.RowStyles.Add(new(SizeType.Percent, 45)); panel.RowStyles.Add(new(SizeType.Absolute, 42)); panel.RowStyles.Add(new(SizeType.Absolute, 60)); panel.RowStyles.Add(new(SizeType.Absolute, 32));
        panel.Controls.Add(new Label { Text = L.T("compose_heading"), Font = new(L.FontName, 18, FontStyle.Bold), AutoSize = true });
        panel.Controls.Add(composeTitle); panel.Controls.Add(composeBody);
        panel.Controls.Add(new Label { Text = L.T("media_heading"), AutoSize = true }); panel.Controls.Add(composeMedia);
        var mediaButtons = new FlowLayoutPanel { Dock = DockStyle.Fill }; mediaButtons.Controls.Add(addMedia); mediaButtons.Controls.Add(removeMedia); mediaButtons.Controls.Add(composeDraft); panel.Controls.Add(mediaButtons);
        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 }; actions.ColumnStyles.Add(new(SizeType.Percent, 65)); actions.ColumnStyles.Add(new(SizeType.Percent, 35)); actions.Controls.Add(publishArticle, 0, 0); actions.Controls.Add(stopCompose, 1, 0); panel.Controls.Add(actions);
        panel.Controls.Add(composeStatus);
        return panel;
    }

    void AddComposerMedia(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Multiselect = true, Title = L.T("media_dialog_title"), Filter = L.T("media_filter") };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        foreach (var path in dialog.FileNames) if (!composeMedia.Items.Contains(path)) composeMedia.Items.Add(path);
        composePostKey = "";
    }

    async void PublishArticle(object? sender, EventArgs e)
    {
        if (cts is not null) return;
        var body = composeBody.Text.Trim();
        var paths = composeMedia.Items.Cast<string>().ToList();
        if (body.Length == 0 && paths.Count == 0) { MessageBox.Show(L.T("content_required"), "FB2Blogger"); return; }
        if (string.IsNullOrWhiteSpace(settings.BlogId) && !await ConfigureFirstRunAsync()) return;

        var title = composeTitle.Text.Trim();
        if (title.Length == 0)
        {
            title = body.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? L.T("new_post_title", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            if (title.Length > 90) title = title[..90] + "…";
        }
        var isRetry = composePostKey.Length > 0;
        if (!isRetry) composePostKey = "manual-" + Guid.NewGuid().ToString("N");
        var post = new FacebookPost(composePostKey, title, body, DateTimeOffset.Now, FacebookParser.ExtractLabels(body), []);
        cts = new(); ToggleBusy(true);
        try
        {
            using var api = new GoogleApi(settings, Say); await api.EnsureAuthorizedAsync(cts.Token);
            if (isRetry)
            {
                Say(L.T("checking_previous_publish"));
                var existing = await api.GetAllPostsAsync(settings.BlogId, cts.Token);
                if (existing.Any(p => p.MigrationKey == post.Key))
                {
                    composeTitle.Clear(); composeBody.Clear(); composeMedia.Items.Clear(); composeMediaCache.Clear(); composePostKey = "";
                    Say(L.T("duplicate_avoided"));
                    MessageBox.Show(L.T("post_already_exists"), "FB2Blogger", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            var html = new StringBuilder($"<!-- FB2BLOGGER:{post.Key} -->");
            if (body.Length > 0) html.Append("<div style=\"white-space:pre-wrap\">").Append(WebUtility.HtmlEncode(body)).Append("</div>");
            List<YouTubeVideoInfo> videos = []; var claimed = new HashSet<string>(StringComparer.Ordinal);
            if (paths.Any(IsVideoPath)) { Say(L.T("checking_youtube")); videos = await api.GetUploadedVideosAsync(cts.Token); }
            foreach (var path in paths)
            {
                cts.Token.ThrowIfCancellationRequested();
                if (!File.Exists(path)) throw new FileNotFoundException(L.T("media_not_found"), path);
                var video = IsVideoPath(path); var item = new MediaItem(Path.GetFileName(path), video);
                var cacheKey = ComposerCacheKey(path);
                if (!composeMediaCache.TryGetValue(cacheKey, out var hosted))
                {
                    if (video)
                    {
                        var found = FindExistingVideo(videos, claimed, post, item, path);
                        if (found is not null) { Say(L.T("reuse_youtube", Path.GetFileName(path))); hosted = new() { Kind = "youtube", Value = found.Id }; claimed.Add(found.Id); }
                        else { Say(L.T("uploading_video", Path.GetFileName(path))); var description = YouTubeDescription(post, item); hosted = new() { Kind = "youtube", Value = await api.UploadVideoAsync(path, title, description, settings.VideoPrivacy, cts.Token) }; claimed.Add(hosted.Value); videos.Add(new(hosted.Value, title, description, Path.GetFileName(path), new FileInfo(path).Length)); }
                    }
                    else { Say(L.T("uploading_image", Path.GetFileName(path))); hosted = await UploadOptimizedImageAsync(api, path, cts.Token); }
                    composeMediaCache[cacheKey] = hosted;
                }
                else Say(L.T("reuse_media", Path.GetFileName(path)));

                if (video) html.Append($"<div style=\"margin:16px 0\"><iframe width=\"560\" height=\"315\" src=\"https://www.youtube.com/embed/{WebUtility.HtmlEncode(hosted.Value)}\" title=\"YouTube video\" frameborder=\"0\" allowfullscreen></iframe></div>");
                else html.Append($"<p><img src=\"{WebUtility.HtmlEncode(hosted.Value)}\" alt=\"{WebUtility.HtmlEncode(L.T("article_image_alt"))}\" style=\"max-width:100%;height:auto\"></p>");
            }
            Say(composeDraft.Checked ? L.T("saving_draft") : L.T("publishing_post"));
            await api.CreatePostAsync(settings.BlogId, post, html.ToString(), composeDraft.Checked, cts.Token);
            composeTitle.Clear(); composeBody.Clear(); composeMedia.Items.Clear(); composeMediaCache.Clear(); composePostKey = "";
            Say(composeDraft.Checked ? L.T("draft_saved") : L.T("post_published"));
            MessageBox.Show(composeDraft.Checked ? L.T("draft_saved") : L.T("post_published"), "FB2Blogger", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException) { Say(L.T("publish_paused")); }
        catch (GoogleQuotaException ex) { Say(ex.Message); MessageBox.Show(ex.Message, L.T("quota_limit"), MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { Say(L.T("publish_failed_detail", ex.Message)); MessageBox.Show(ex.Message, L.T("publish_failed"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { cts.Dispose(); cts = null; ToggleBusy(false); }
    }

    static bool IsVideoPath(string path) => Path.GetExtension(path).ToLowerInvariant() is ".mp4" or ".mov" or ".m4v" or ".avi" or ".mkv" or ".webm";
    static string ComposerCacheKey(string path) { var file = new FileInfo(path); return $"{Path.GetFullPath(path)}|{file.Length}|{file.LastWriteTimeUtc.Ticks}"; }

    async Task<HostedMedia> UploadOptimizedImageAsync(GoogleApi api, string originalPath, CancellationToken ct)
    {
        using var optimized = await Task.Run(() => ImageOptimizer.Prepare(originalPath), ct);
        if (optimized.IsTemporary) Say(L.T("image_reduced", FormatBytes(optimized.OriginalBytes), FormatBytes(optimized.UploadBytes)));
        var url = await api.UploadImageAsync(optimized.Path, ct, Path.GetFileName(originalPath));
        return new() { Kind = "image", Value = url, Optimized = true };
    }

    void ChooseZip(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = L.T("zip_filter"), Title = L.T("choose_export_zip") };
        if (dialog.ShowDialog(this) == DialogResult.OK) { zipPath.Text = dialog.FileName; start.Enabled = true; Say(L.T("zip_selected")); }
    }

    async void Configure(object? sender, EventArgs e) => await ConfigureFirstRunAsync();

    void RequestPause(object? sender, EventArgs e)
    {
        if (cts is null) return;
        stop.Enabled = false;
        status.Text = L.T("pausing_safely");
        log.AppendText($"[{DateTime.Now:HH:mm:ss}] {L.T("pause_requested")}\r\n");
        cts.Cancel();
    }

    void HandleFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason is CloseReason.WindowsShutDown or CloseReason.TaskManagerClosing)
        {
            cts?.Cancel();
            return;
        }
        if (cts is not null)
        {
            e.Cancel = true;
            RequestPause(sender, EventArgs.Empty);
            MessageBox.Show(L.T("wait_before_close"), "FB2Blogger", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (composeTitle.TextLength > 0 || composeBody.TextLength > 0 || composeMedia.Items.Count > 0)
        {
            var answer = MessageBox.Show(L.T("discard_unpublished"), L.T("unpublished"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes) e.Cancel = true;
        }
    }

    async Task<bool> ConfigureFirstRunAsync()
    {
        var previousLanguage = settings.InterfaceLanguage;
        using var dialog = new SetupDialog(settings); if (dialog.ShowDialog(this) != DialogResult.OK) return false;
        if (!string.Equals(previousLanguage, settings.InterfaceLanguage, StringComparison.OrdinalIgnoreCase))
        {
            L.Configure(settings.InterfaceLanguage);
            MessageBox.Show(L.T("language_changed"), "FB2Blogger", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Application.Restart();
            return false;
        }
        try
        {
            ToggleBusy(true); using var api = new GoogleApi(settings, Say); await api.EnsureAuthorizedAsync(CancellationToken.None);
            var blogs = await api.GetBlogsAsync(CancellationToken.None);
            if (blogs.Count == 0) throw new InvalidOperationException(L.T("blogger_site_missing"));
            BlogInfo selected = blogs[0];
            if (blogs.Count > 1)
            {
                using var picker = new Form { Text = L.T("choose_blogger_site"), Width = 430, Height = 150, StartPosition = FormStartPosition.CenterParent, Font = Font, FormBorderStyle = FormBorderStyle.FixedDialog };
                var combo = new ComboBox { DataSource = blogs, DropDownStyle = ComboBoxStyle.DropDownList, Width = 370, Location = new(20, 18) }; var ok = new Button { Text = L.T("ok"), DialogResult = DialogResult.OK, Location = new(315, 57) }; picker.Controls.Add(combo); picker.Controls.Add(ok); picker.AcceptButton = ok;
                if (picker.ShowDialog(this) != DialogResult.OK) return false; selected = (BlogInfo)combo.SelectedItem!;
            }
            settings.BlogId = selected.Id; settings.BlogName = selected.Name; SettingsStore.Save(settings); Say(L.T("setup_complete", selected.Name)); return true;
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, L.T("setup_failed"), MessageBoxButtons.OK, MessageBoxIcon.Error); Say(L.T("setup_failed_detail", ex.Message)); return false; }
        finally { ToggleBusy(false); }
    }

    async void StartMigration(object? sender, EventArgs e)
    {
        if (!File.Exists(zipPath.Text)) { MessageBox.Show(L.T("choose_zip_first")); return; }
        if (string.IsNullOrWhiteSpace(settings.BlogId) && !await ConfigureFirstRunAsync()) return;
        cts = new(); ToggleBusy(true); var report = new MigrationReport(); var temp = Path.Combine(Path.GetTempPath(), "FB2Blogger", Guid.NewGuid().ToString("N"));
        try
        {
            Say(L.T("cleaning_stale_temp")); await Task.Run(CleanupStaleTemps, cts.Token);
            Directory.CreateDirectory(temp); Say(L.T("extracting_facebook_zip")); await Task.Run(() => SafeExtract(zipPath.Text, temp, cts.Token), cts.Token);
            Say(L.T("finding_export_content")); var posts = await Task.Run(() => FacebookParser.Read(temp, Say, cts.Token), cts.Token); report.Total = posts.Count;
            if (posts.Count == 0) throw new InvalidOperationException(L.T("facebook_posts_missing"));
            var legacyStateFile = SettingsStore.StateFile(zipPath.Text);
            var legacyCompleted = File.Exists(legacyStateFile) ? File.ReadAllLines(legacyStateFile).ToHashSet() : [];
            var migration = SettingsStore.LoadMigration(zipPath.Text);
            using var api = new GoogleApi(settings, Say); await api.EnsureAuthorizedAsync(cts.Token);

            Say(L.T("checking_blogger_existing"));
            var bloggerPosts = await api.GetAllPostsAsync(settings.BlogId, cts.Token);
            var blogByKey = bloggerPosts.Where(p => p.MigrationKey.Length > 0).GroupBy(p => p.MigrationKey).ToDictionary(g => g.Key, g => g.First());
            var blogBySecond = bloggerPosts.GroupBy(p => p.Published.ToUnixTimeSeconds()).ToDictionary(g => g.Key, g => g.First());
            foreach (var post in posts)
            {
                if (!migration.Posts.TryGetValue(post.Key, out var saved)) migration.Posts[post.Key] = saved = new();
                if (blogByKey.TryGetValue(post.Key, out var existing) || blogBySecond.TryGetValue(post.Published.ToUnixTimeSeconds(), out existing))
                {
                    saved.Complete = true; saved.BloggerPostId = existing.Id;
                }
                else if (saved.Complete || legacyCompleted.Contains(post.Key))
                {
                    // It was previously completed but no longer exists on Blogger:
                    // rebuild it while retaining any cached uploaded media.
                    saved.Complete = false; saved.BloggerPostId = "";
                }
            }
            SettingsStore.SaveMigration(zipPath.Text, migration);

            if (posts.Any(p => p.Media.Any(m => !m.IsVideo)))
                await OptimizeExistingDriveImagesAsync(api, posts, migration, temp, cts.Token);

            List<YouTubeVideoInfo> youtubeVideos = [];
            var claimedYouTubeIds = new HashSet<string>(StringComparer.Ordinal);
            if (posts.Any(p => p.Media.Any(m => m.IsVideo)))
            {
                Say(L.T("checking_youtube_existing"));
                youtubeVideos = await api.GetUploadedVideosAsync(cts.Token);
            }
            for (var index = 0; index < posts.Count; index++)
            {
                cts.Token.ThrowIfCancellationRequested(); var post = posts[index];
                var postState = migration.Posts[post.Key];
                if (postState.Complete) { report.Skipped++; UpdateProgress(index + 1, posts.Count, L.T("skip_existing_post", index + 1, posts.Count)); continue; }
                try
                {
                    Say(L.T("migrating_post", index + 1, posts.Count, post.Title));
                    var html = new StringBuilder($"<!-- FB2BLOGGER:{WebUtility.HtmlEncode(post.Key)} -->");
                    if (!string.IsNullOrWhiteSpace(post.Text)) html.Append("<div style=\"white-space:pre-wrap\">").Append(WebUtility.HtmlEncode(post.Text)).Append("</div>");
                    foreach (var media in post.Media)
                    {
                        var path = ResolveMedia(temp, media.RelativePath); if (path is null) { Say(L.T("media_path_missing", media.RelativePath)); continue; }
                        if (postState.Media.TryGetValue(media.RelativePath, out var hosted) && hosted.Value.Length > 0)
                        {
                            Say(media.IsVideo ? L.T("reuse_previous_youtube") : L.T("reuse_previous_image"));
                        }
                        else if (media.IsVideo)
                        {
                            var description = YouTubeDescription(post, media);
                            var found = FindExistingVideo(youtubeVideos, claimedYouTubeIds, post, media, path);
                            if (found is not null) { Say(L.T("found_existing_youtube")); hosted = new() { Kind = "youtube", Value = found.Id }; claimedYouTubeIds.Add(found.Id); }
                            else { Say(L.T("upload_video_to_youtube")); hosted = new() { Kind = "youtube", Value = await api.UploadVideoAsync(path, post.Title, description, settings.VideoPrivacy, cts.Token) }; youtubeVideos.Add(new(hosted.Value, post.Title, description, Path.GetFileName(path), new FileInfo(path).Length)); claimedYouTubeIds.Add(hosted.Value); report.Videos++; }
                            postState.Media[media.RelativePath] = hosted; SettingsStore.SaveMigration(zipPath.Text, migration);
                        }
                        else
                        {
                            Say(L.T("optimize_upload_image")); hosted = await UploadOptimizedImageAsync(api, path, cts.Token); report.Images++;
                            postState.Media[media.RelativePath] = hosted; SettingsStore.SaveMigration(zipPath.Text, migration);
                        }

                        if (media.IsVideo) html.Append($"<div style=\"margin:16px 0\"><iframe width=\"560\" height=\"315\" src=\"https://www.youtube.com/embed/{WebUtility.HtmlEncode(hosted.Value)}\" title=\"YouTube video\" frameborder=\"0\" allowfullscreen></iframe></div>");
                        else html.Append($"<p><img src=\"{WebUtility.HtmlEncode(hosted.Value)}\" alt=\"{WebUtility.HtmlEncode(L.T("facebook_image_alt"))}\" style=\"max-width:100%;height:auto\"></p>");
                    }
                    postState.BloggerPostId = await api.CreatePostAsync(settings.BlogId, post, html.ToString(), settings.CreateAsDraft, cts.Token);
                    postState.Complete = true; SettingsStore.SaveMigration(zipPath.Text, migration);
                    if (!legacyCompleted.Contains(post.Key)) { await File.AppendAllLinesAsync(legacyStateFile, [post.Key], cts.Token); legacyCompleted.Add(post.Key); }
                    report.Imported++;
                    // Avoid Blogger's per-user write burst limit during large migrations.
                    await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);
                }
                catch (GoogleQuotaException) { throw; }
                catch (Exception ex) when (ex is not OperationCanceledException) { report.Failed++; report.Errors.Add($"{post.Published:yyyy-MM-dd} {post.Title}：{ex.Message}"); Say(L.T("post_failed_continue", ex.Message)); }
                UpdateProgress(index + 1, posts.Count, L.T("completed_progress", index + 1, posts.Count));
            }
            ShowReport(report, false);
        }
        catch (OperationCanceledException) { Say(L.T("migration_stopped_resume")); ShowReport(report, true); }
        catch (GoogleQuotaException ex) { Say(ex.Message); ShowReport(report, true); MessageBox.Show(ex.Message, L.T("google_quota_paused"), MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { Say(L.T("migration_failed", ex.Message)); MessageBox.Show(ex.Message, "FB2Blogger", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { try { Directory.Delete(temp, true); } catch { } cts.Dispose(); cts = null; ToggleBusy(false); }
    }

    void ShowReport(MigrationReport r, bool stopped)
    {
        var state = stopped ? L.T("stopped") : L.T("completed");
        var text = L.T("migration_report_text", r.Started, DateTime.Now, state, r.Total, r.Imported, r.Skipped, r.Failed, r.Images, r.Videos) + (r.Errors.Count > 0 ? L.T("failure_details") + string.Join("\r\n", r.Errors) : "");
        var reportNote = L.T("report_write_failed");
        try
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FB2Blogger Reports");
            Directory.CreateDirectory(folder); var path = Path.Combine(folder, L.T("report_file_name", DateTime.Now));
            File.WriteAllText(path, text, Encoding.UTF8); reportNote = L.T("report_saved");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        Say(stopped ? L.T("migration_stopped_summary", reportNote) : L.T("migration_completed_summary", r.Imported, r.Skipped, r.Failed, reportNote));
        MessageBox.Show(L.T("migration_report_dialog", stopped ? L.T("stopped") : L.T("migration_completed"), r.Imported, r.Skipped, r.Failed, r.Images, r.Videos, reportNote), L.T("migration_report_title"), MessageBoxButtons.OK, r.Failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }

    static string? ResolveMedia(string root, string relative)
    {
        var normalized = relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); var candidate = Path.GetFullPath(Path.Combine(root, normalized)); var fullRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        if (candidate.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(candidate)) return candidate;
        var name = Path.GetFileName(relative); return string.IsNullOrEmpty(name) ? null : Directory.EnumerateFiles(root, name, SearchOption.AllDirectories).FirstOrDefault();
    }

    static string YouTubeDescription(FacebookPost post, MediaItem media)
    {
        var marker = $"\n\n[FB2Blogger:{post.Key}:{media.RelativePath.Replace('\\', '/')} ]";
        var maxText = Math.Max(0, 5000 - marker.Length);
        var text = post.Text.Length <= maxText ? post.Text : post.Text[..maxText];
        if (text.Length > 0 && char.IsHighSurrogate(text[^1])) text = text[..^1];
        return text + marker;
    }

    async Task OptimizeExistingDriveImagesAsync(GoogleApi api, List<FacebookPost> posts, MigrationState migration, string extractedRoot, CancellationToken ct)
    {
        Say(L.T("checking_previous_images"));
        var driveImages = await api.GetDriveImagesAsync(ct);
        var processedIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var post in posts)
        {
            ct.ThrowIfCancellationRequested();
            var postState = migration.Posts[post.Key];
            foreach (var media in post.Media.Where(m => !m.IsVideo))
            {
                var source = ResolveMedia(extractedRoot, media.RelativePath); if (source is null) continue;
                postState.Media.TryGetValue(media.RelativePath, out var hosted);
                if (hosted is null || hosted.Value.Length == 0)
                {
                    var file = new FileInfo(source);
                    var candidates = driveImages.Where(x => x.Size == file.Length && string.Equals(x.Name, file.Name, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (candidates.Count > 0)
                    {
                        await using var stream = File.OpenRead(source);
                        var md5 = Convert.ToHexString(await MD5.HashDataAsync(stream, ct));
                        var match = candidates.FirstOrDefault(x => string.Equals(x.Md5Checksum, md5, StringComparison.OrdinalIgnoreCase));
                        if (match is not null)
                        {
                            hosted = new() { Kind = "image", Value = $"https://drive.google.com/thumbnail?id={match.Id}&sz=w1600" };
                            postState.Media[media.RelativePath] = hosted;
                        }
                    }
                }
                if (hosted is null || hosted.Optimized) continue;
                var id = DriveIdFromUrl(hosted.Value); if (id.Length == 0) continue;
                if (!processedIds.Add(id))
                {
                    hosted.Optimized = true; SettingsStore.SaveMigration(zipPath.Text, migration); continue;
                }
                using var optimized = await Task.Run(() => ImageOptimizer.Prepare(source), ct);
                if (optimized.IsTemporary)
                {
                    Say(L.T("optimizing_existing_image", Path.GetFileName(source), FormatBytes(optimized.OriginalBytes), FormatBytes(optimized.UploadBytes)));
                    await api.UpdateDriveImageAsync(id, optimized.Path, ct);
                }
                hosted.Optimized = true;
                SettingsStore.SaveMigration(zipPath.Text, migration);
            }
        }
    }

    static string DriveIdFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return "";
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && parts[0] == "id") return Uri.UnescapeDataString(parts[1]);
        }
        return "";
    }

    static YouTubeVideoInfo? FindExistingVideo(List<YouTubeVideoInfo> videos, HashSet<string> claimedIds, FacebookPost post, MediaItem media, string localPath)
    {
        // Strong fingerprint for videos previously uploaded manually: YouTube
        // exposes the owner's original upload file name and exact byte size.
        var localName = Path.GetFileName(localPath).Normalize(NormalizationForm.FormC);
        var localSize = new FileInfo(localPath).Length;
        var fingerprint = videos.FirstOrDefault(v =>
            !claimedIds.Contains(v.Id) && v.OriginalFileSize == localSize &&
            v.OriginalFileName.Length > 0 &&
            string.Equals(v.OriginalFileName.Normalize(NormalizationForm.FormC), localName, StringComparison.OrdinalIgnoreCase));
        if (fingerprint is not null) return fingerprint;

        var markedDescription = YouTubeDescription(post, media);
        var exact = videos.FirstOrDefault(v => !claimedIds.Contains(v.Id) && v.Title == post.Title && v.Description == markedDescription);
        if (exact is not null) return exact;
        // Version 2 used the post text only. Claim each matching upload once so a
        // multi-video Facebook post cannot reuse one YouTube ID for every video.
        exact = videos.FirstOrDefault(v => !claimedIds.Contains(v.Id) && v.Title == post.Title && v.Description == post.Text);
        if (exact is not null) return exact;
        // Version 1 uploaded text before repairing Facebook's legacy UTF-8-as-Latin1 encoding.
        try
        {
            var oldTitle = Encoding.Latin1.GetString(Encoding.UTF8.GetBytes(post.Title));
            var oldText = Encoding.Latin1.GetString(Encoding.UTF8.GetBytes(post.Text));
            return videos.FirstOrDefault(v => !claimedIds.Contains(v.Id) && v.Title == oldTitle && v.Description == oldText);
        }
        catch { return null; }
    }

    static void SafeExtract(string archive, string target, CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(target) + Path.DirectorySeparatorChar;
        using var zip = ZipFile.OpenRead(archive);
        if (zip.Entries.Count > 250_000) throw new InvalidDataException(L.T("zip_too_many_entries"));

        long totalBytes = 0;
        foreach (var entry in zip.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry.FullName.Length > 1024) throw new InvalidDataException(L.T("zip_file_name_too_long"));
            try { totalBytes = checked(totalBytes + entry.Length); }
            catch (OverflowException) { throw new InvalidDataException(L.T("zip_size_invalid")); }
        }

        var archiveBytes = Math.Max(1, new FileInfo(archive).Length);
        if (totalBytes > 10L * 1024 * 1024 * 1024 && totalBytes / archiveBytes > 500)
            throw new InvalidDataException(L.T("zip_ratio_invalid"));
        var driveRoot = Path.GetPathRoot(root) ?? root;
        var available = new DriveInfo(driveRoot).AvailableFreeSpace;
        var reserve = 2L * 1024 * 1024 * 1024;
        if (totalBytes > Math.Max(0, available - reserve))
            throw new IOException(L.T("disk_space_insufficient", FormatBytes(totalBytes), FormatBytes(Math.Max(0, available - reserve))));

        foreach (var entry in zip.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.GetFullPath(Path.Combine(target, entry.FullName));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException(L.T("zip_unsafe_path"));
            if (string.IsNullOrEmpty(entry.Name)) Directory.CreateDirectory(path);
            else { Directory.CreateDirectory(Path.GetDirectoryName(path)!); entry.ExtractToFile(path, true); }
        }
    }

    static void CleanupStaleTemps()
    {
        var root = Path.Combine(Path.GetTempPath(), "FB2Blogger");
        if (!Directory.Exists(root)) return;
        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            try { Directory.Delete(directory, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    static string FormatBytes(long bytes) => bytes >= 1024L * 1024 * 1024
        ? $"{bytes / (1024d * 1024 * 1024):0.0} GB"
        : $"{bytes / (1024d * 1024):0.0} MB";

    void ToggleBusy(bool busy)
    {
        choose.Enabled = !busy; start.Enabled = !busy && File.Exists(zipPath.Text); settingsButton.Enabled = !busy;
        composeTitle.Enabled = !busy; composeBody.Enabled = !busy; composeMedia.Enabled = !busy; addMedia.Enabled = !busy; removeMedia.Enabled = !busy; composeDraft.Enabled = !busy; publishArticle.Enabled = !busy;
        stop.Enabled = busy; stopCompose.Enabled = busy; UseWaitCursor = busy;
    }
    void UpdateProgress(int current, int total, string message) { progress.Value = total == 0 ? 0 : Math.Clamp(current * 100 / total, 0, 100); Say(message); }
    void Say(string message) { if (InvokeRequired) { BeginInvoke(() => Say(message)); return; } status.Text = message; composeStatus.Text = message; log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n"); }
}
