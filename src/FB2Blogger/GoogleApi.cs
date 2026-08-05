using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace FB2Blogger;

internal sealed class GoogleQuotaException(string message) : Exception(message);

internal sealed class GoogleApi : IDisposable
{
    const int ScopeVersion = 2;
    const string Scopes = "https://www.googleapis.com/auth/blogger https://www.googleapis.com/auth/youtube.upload https://www.googleapis.com/auth/youtube.readonly https://www.googleapis.com/auth/drive.file";
    readonly HttpClient http = new() { Timeout = TimeSpan.FromHours(4) };
    readonly AppSettings settings;
    readonly Action<string> log;
    string accessToken = "";
    DateTime tokenExpires;

    public GoogleApi(AppSettings settings, Action<string> log) { this.settings = settings; this.log = log; }

    public async Task EnsureAuthorizedAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(settings.RefreshToken) && settings.AuthorizedScopeVersion >= ScopeVersion)
        {
            try { await RefreshAsync(ct); return; }
            catch { log("已保存的 Google 登入失效，需要重新授權一次。 "); }
        }
        await InteractiveAuthorizeAsync(ct);
    }

    async Task InteractiveAuthorizeAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(settings.ClientId)) throw new InvalidOperationException("尚未設定 Google OAuth Client ID。");
        var verifier = Base64(RandomNumberGenerator.GetBytes(48));
        var challenge = Base64(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = Base64(RandomNumberGenerator.GetBytes(24));
        var port = FreePort(); var redirect = $"http://127.0.0.1:{port}/";
        using var listener = new HttpListener(); listener.Prefixes.Add(redirect); listener.Start();
        var url = "https://accounts.google.com/o/oauth2/v2/auth?" +
                  $"client_id={Uri.EscapeDataString(settings.ClientId)}&redirect_uri={Uri.EscapeDataString(redirect)}&response_type=code" +
                  $"&scope={Uri.EscapeDataString(Scopes)}&access_type=offline&prompt=consent&code_challenge={challenge}&code_challenge_method=S256&state={state}";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        log("第一次使用：請在瀏覽器允許 Google 存取。完成後以後會自動登入。");
        var context = await listener.GetContextAsync().WaitAsync(TimeSpan.FromMinutes(10), ct);
        var code = context.Request.QueryString["code"];
        var returnedState = context.Request.QueryString["state"];
        var reply = Encoding.UTF8.GetBytes("<html><meta charset='utf-8'><body style='font-family:sans-serif;padding:40px'><h2>授權完成</h2><p>請關閉此頁並回到 FB2Blogger。</p></body></html>");
        context.Response.ContentType = "text/html; charset=utf-8"; context.Response.ContentLength64 = reply.Length;
        await context.Response.OutputStream.WriteAsync(reply, ct); context.Response.Close(); listener.Stop();
        if (string.IsNullOrEmpty(code) || returnedState != state) throw new InvalidOperationException("Google 授權被取消或驗證失敗。");

        var form = new Dictionary<string, string> { ["client_id"] = settings.ClientId, ["code"] = code, ["code_verifier"] = verifier, ["redirect_uri"] = redirect, ["grant_type"] = "authorization_code" };
        if (!string.IsNullOrWhiteSpace(settings.ClientSecret)) form["client_secret"] = settings.ClientSecret;
        var json = await PostTokenAsync(form, ct);
        ApplyToken(json);
        settings.RefreshToken = json["refresh_token"]?.GetValue<string>() ?? throw new InvalidOperationException("Google 未提供長期登入憑證，請撤銷應用程式權限後重試。");
        settings.AuthorizedScopeVersion = ScopeVersion;
        SettingsStore.Save(settings);
    }

    async Task RefreshAsync(CancellationToken ct)
    {
        var form = new Dictionary<string, string> { ["client_id"] = settings.ClientId, ["refresh_token"] = settings.RefreshToken, ["grant_type"] = "refresh_token" };
        if (!string.IsNullOrWhiteSpace(settings.ClientSecret)) form["client_secret"] = settings.ClientSecret;
        ApplyToken(await PostTokenAsync(form, ct));
    }

    async Task<JsonNode> PostTokenAsync(Dictionary<string, string> form, CancellationToken ct)
    {
        using var formContent = new FormUrlEncodedContent(form);
        using var response = await http.PostAsync("https://oauth2.googleapis.com/token", formContent, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("Google 登入失敗：" + FriendlyError(body));
        return JsonNode.Parse(body) ?? throw new InvalidOperationException("Google 登入回應無效。");
    }

    void ApplyToken(JsonNode json)
    {
        accessToken = json["access_token"]?.GetValue<string>() ?? throw new InvalidOperationException("Google 沒有回傳 access token。");
        tokenExpires = DateTime.UtcNow.AddSeconds(json["expires_in"]?.GetValue<int>() ?? 3500);
    }

    async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, HttpContent? content, CancellationToken ct, HttpCompletionOption option = HttpCompletionOption.ResponseContentRead)
    {
        if (DateTime.UtcNow >= tokenExpires.AddMinutes(-2)) await RefreshAsync(ct);
        using var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await http.SendAsync(request, option, ct);
    }

    public async Task<List<BlogInfo>> GetBlogsAsync(CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Get, "https://www.googleapis.com/blogger/v3/users/self/blogs", null, ct);
        var body = await response.Content.ReadAsStringAsync(ct); Ensure(response, body, "讀取 Blogger");
        return JsonNode.Parse(body)?["items"]?.AsArray().Select(x => new BlogInfo(x!["id"]!.GetValue<string>(), x["name"]!.GetValue<string>())).ToList() ?? [];
    }

    public async Task<string> UploadImageAsync(string path, CancellationToken ct, string? displayName = null)
    {
        var boundary = "fb2blogger_" + Guid.NewGuid().ToString("N");
        using var content = new MultipartContent("related", boundary);
        content.Add(new StringContent(new JsonObject { ["name"] = displayName ?? Path.GetFileName(path) }.ToJsonString(), Encoding.UTF8, "application/json"));
        await using var file = File.OpenRead(path); var stream = new StreamContent(file); stream.Headers.ContentType = new MediaTypeHeaderValue(Mime(path)); content.Add(stream);
        using var response = await SendAsync(HttpMethod.Post, "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart&fields=id", content, ct);
        var body = await response.Content.ReadAsStringAsync(ct); Ensure(response, body, "上傳圖片");
        var id = JsonNode.Parse(body)?["id"]?.GetValue<string>() ?? throw new InvalidOperationException("Google Drive 未回傳圖片 ID。");
        using var permission = new StringContent(new JsonObject { ["type"] = "anyone", ["role"] = "reader" }.ToJsonString(), Encoding.UTF8, "application/json");
        using var permResponse = await SendAsync(HttpMethod.Post, $"https://www.googleapis.com/drive/v3/files/{id}/permissions", permission, ct);
        var permBody = await permResponse.Content.ReadAsStringAsync(ct); Ensure(permResponse, permBody, "設定圖片顯示權限");
        return $"https://drive.google.com/thumbnail?id={id}&sz=w1600";
    }

    public async Task<List<DriveImageInfo>> GetDriveImagesAsync(CancellationToken ct)
    {
        var output = new List<DriveImageInfo>(); string pageToken = "";
        do
        {
            var url = "https://www.googleapis.com/drive/v3/files?spaces=drive&q=trashed%3Dfalse&fields=nextPageToken%2Cfiles(id%2Cname%2Csize%2Cmd5Checksum%2CmimeType)&pageSize=1000";
            if (pageToken.Length > 0) url += "&pageToken=" + Uri.EscapeDataString(pageToken);
            using var response = await SendAsync(HttpMethod.Get, url, null, ct); var body = await response.Content.ReadAsStringAsync(ct); Ensure(response, body, "檢查 Drive 圖片");
            var root = JsonNode.Parse(body); pageToken = root?["nextPageToken"]?.GetValue<string>() ?? "";
            foreach (var item in root?["files"]?.AsArray() ?? [])
            {
                var mime = item?["mimeType"]?.GetValue<string>() ?? ""; if (!mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) continue;
                _ = long.TryParse(item?["size"]?.ToString().Trim('"'), out var size);
                output.Add(new(item?["id"]?.GetValue<string>() ?? "", item?["name"]?.GetValue<string>() ?? "", size, item?["md5Checksum"]?.GetValue<string>() ?? ""));
            }
        } while (pageToken.Length > 0);
        return output;
    }

    public async Task UpdateDriveImageAsync(string fileId, string path, CancellationToken ct)
    {
        await using var file = File.OpenRead(path); using var content = new StreamContent(file); content.Headers.ContentType = new MediaTypeHeaderValue(Mime(path));
        using var response = await SendAsync(HttpMethod.Patch, $"https://www.googleapis.com/upload/drive/v3/files/{Uri.EscapeDataString(fileId)}?uploadType=media", content, ct);
        var body = await response.Content.ReadAsStringAsync(ct); Ensure(response, body, "壓縮既有 Drive 圖片");
    }

    public async Task<string> UploadVideoAsync(string path, string title, string description, string privacy, CancellationToken ct)
    {
        var metadata = new JsonObject { ["snippet"] = new JsonObject { ["title"] = title, ["description"] = description }, ["status"] = new JsonObject { ["privacyStatus"] = privacy, ["selfDeclaredMadeForKids"] = false } };
        using var initContent = new StringContent(metadata.ToJsonString(), Encoding.UTF8, "application/json");
        using var init = await SendAsync(HttpMethod.Post, "https://www.googleapis.com/upload/youtube/v3/videos?uploadType=resumable&part=snippet,status", initContent, ct);
        var initBody = await init.Content.ReadAsStringAsync(ct); Ensure(init, initBody, "啟動 YouTube 上傳");
        var uploadUrl = init.Headers.Location ?? throw new InvalidOperationException("YouTube 未提供上傳網址。");
        await using var file = File.OpenRead(path); using var video = new StreamContent(file); video.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        using var response = await SendAsync(HttpMethod.Put, uploadUrl.ToString(), video, ct, HttpCompletionOption.ResponseHeadersRead);
        var body = await response.Content.ReadAsStringAsync(ct); Ensure(response, body, "上傳 YouTube 影片");
        return JsonNode.Parse(body)?["id"]?.GetValue<string>() ?? throw new InvalidOperationException("YouTube 未回傳影片 ID。");
    }

    public async Task<string> CreatePostAsync(string blogId, FacebookPost post, string html, bool draft, CancellationToken ct)
    {
        var labels = new JsonArray(); foreach (var label in post.Labels) labels.Add(label);
        var payload = new JsonObject { ["kind"] = "blogger#post", ["blog"] = new JsonObject { ["id"] = blogId }, ["title"] = post.Title, ["content"] = html, ["published"] = post.Published.UtcDateTime.ToString("O"), ["labels"] = labels };
        var delays = new[] { 5, 15, 30, 60 };
        for (var attempt = 0; ; attempt++)
        {
            using var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
            using var response = await SendAsync(HttpMethod.Post, $"https://www.googleapis.com/blogger/v3/blogs/{blogId}/posts?isDraft={draft.ToString().ToLowerInvariant()}", content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (response.IsSuccessStatusCode)
                return JsonNode.Parse(body)?["id"]?.GetValue<string>() ?? "";
            if (!IsQuota(response, body)) Ensure(response, body, "建立 Blogger 文章");
            if (attempt >= delays.Length)
                throw new GoogleQuotaException("Blogger 目前的新增文章配額已用完。程式已安全停止；請稍後或明天再選同一個 ZIP，會從未完成處繼續。");
            var seconds = response.Headers.RetryAfter?.Delta is { } retry ? Math.Max(delays[attempt], (int)retry.TotalSeconds) : delays[attempt];
            log($"Blogger 暫時限制速度，等待 {seconds} 秒後自動重試（{attempt + 1}/{delays.Length}）…");
            await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
        }
    }

    public async Task<List<BloggerPostInfo>> GetAllPostsAsync(string blogId, CancellationToken ct)
    {
        var output = new List<BloggerPostInfo>(); string pageToken = "";
        do
        {
            var url = $"https://www.googleapis.com/blogger/v3/blogs/{blogId}/posts?fetchBodies=true&fetchImages=false&maxResults=500&status=live&status=draft";
            if (pageToken.Length > 0) url += "&pageToken=" + Uri.EscapeDataString(pageToken);
            using var response = await SendAsync(HttpMethod.Get, url, null, ct); var body = await response.Content.ReadAsStringAsync(ct); Ensure(response, body, "檢查 Blogger 文章");
            var root = JsonNode.Parse(body); pageToken = root?["nextPageToken"]?.GetValue<string>() ?? "";
            foreach (var item in root?["items"]?.AsArray() ?? [])
            {
                if (DateTimeOffset.TryParse(item?["published"]?.GetValue<string>(), out var published))
                {
                    var contentBody = item?["content"]?.GetValue<string>() ?? "";
                    output.Add(new(item?["id"]?.GetValue<string>() ?? "", item?["title"]?.GetValue<string>() ?? "", published, ExtractMigrationKey(contentBody)));
                }
            }
        } while (pageToken.Length > 0);
        return output;
    }

    static string ExtractMigrationKey(string html)
    {
        const string prefix = "<!-- FB2BLOGGER:";
        var start = html.IndexOf(prefix, StringComparison.Ordinal);
        if (start < 0) return "";
        start += prefix.Length;
        var end = html.IndexOf(" -->", start, StringComparison.Ordinal);
        return end > start ? WebUtility.HtmlDecode(html[start..end]) : "";
    }

    public async Task<List<YouTubeVideoInfo>> GetUploadedVideosAsync(CancellationToken ct)
    {
        using var channelResponse = await SendAsync(HttpMethod.Get, "https://www.googleapis.com/youtube/v3/channels?part=contentDetails&mine=true", null, ct);
        var channelBody = await channelResponse.Content.ReadAsStringAsync(ct); Ensure(channelResponse, channelBody, "檢查 YouTube 頻道");
        var playlist = JsonNode.Parse(channelBody)?["items"]?[0]?["contentDetails"]?["relatedPlaylists"]?["uploads"]?.GetValue<string>();
        if (string.IsNullOrEmpty(playlist)) return [];
        var ids = new List<string>(); string pageToken = "";
        do
        {
            var url = $"https://www.googleapis.com/youtube/v3/playlistItems?part=snippet&maxResults=50&playlistId={Uri.EscapeDataString(playlist)}";
            if (pageToken.Length > 0) url += "&pageToken=" + Uri.EscapeDataString(pageToken);
            using var response = await SendAsync(HttpMethod.Get, url, null, ct); var body = await response.Content.ReadAsStringAsync(ct); Ensure(response, body, "讀取 YouTube 上傳紀錄");
            var root = JsonNode.Parse(body); pageToken = root?["nextPageToken"]?.GetValue<string>() ?? "";
            foreach (var item in root?["items"]?.AsArray() ?? [])
            {
                var id = item?["snippet"]?["resourceId"]?["videoId"]?.GetValue<string>() ?? "";
                if (id.Length > 0) ids.Add(id);
            }
        } while (pageToken.Length > 0);

        var output = new List<YouTubeVideoInfo>();
        foreach (var batch in ids.Chunk(50))
        {
            var url = "https://www.googleapis.com/youtube/v3/videos?part=snippet,fileDetails&id=" + Uri.EscapeDataString(string.Join(',', batch));
            using var response = await SendAsync(HttpMethod.Get, url, null, ct); var body = await response.Content.ReadAsStringAsync(ct);
            Ensure(response, body, "讀取 YouTube 影片指紋");
            foreach (var item in JsonNode.Parse(body)?["items"]?.AsArray() ?? [])
            {
                var snippet = item?["snippet"]; var details = item?["fileDetails"];
                var sizeText = details?["fileSize"]?.ToString().Trim('"') ?? "0";
                _ = long.TryParse(sizeText, out var size);
                output.Add(new(
                    item?["id"]?.GetValue<string>() ?? "",
                    snippet?["title"]?.GetValue<string>() ?? "",
                    snippet?["description"]?.GetValue<string>() ?? "",
                    details?["fileName"]?.GetValue<string>() ?? "",
                    size));
            }
        }
        return output;
    }

    static bool IsQuota(HttpResponseMessage response, string body) => response.StatusCode == HttpStatusCode.TooManyRequests || body.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase) || body.Contains("Resource has been exhausted", StringComparison.OrdinalIgnoreCase) || body.Contains("quota", StringComparison.OrdinalIgnoreCase) || body.Contains("rateLimitExceeded", StringComparison.OrdinalIgnoreCase);
    static void Ensure(HttpResponseMessage response, string body, string action) { if (!response.IsSuccessStatusCode) { var message = FriendlyError(body); if (IsQuota(response, body)) throw new GoogleQuotaException($"{action}受到 Google 配額限制：{message}"); throw new InvalidOperationException($"{action}失敗：{message}"); } }
    static string FriendlyError(string body) { try { return JsonNode.Parse(body)?["error"]?["message"]?.GetValue<string>() ?? body; } catch { return body.Length > 500 ? body[..500] : body; } }
    static string Mime(string path) => Path.GetExtension(path).ToLowerInvariant() switch { ".png" => "image/png", ".gif" => "image/gif", ".webp" => "image/webp", ".bmp" => "image/bmp", _ => "image/jpeg" };
    static string Base64(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    static int FreePort() { using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return port; }
    public void Dispose() => http.Dispose();
}
