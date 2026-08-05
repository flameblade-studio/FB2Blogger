namespace FB2Blogger;

internal sealed record BlogInfo(string Id, string Name)
{
    public override string ToString() => Name;
}

internal sealed record MediaItem(string RelativePath, bool IsVideo);

internal sealed record FacebookPost(
    string Key,
    string Title,
    string Text,
    DateTimeOffset Published,
    List<string> Labels,
    List<MediaItem> Media);

internal sealed class AppSettings
{
    public string InterfaceLanguage { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public string BlogId { get; set; } = "";
    public string BlogName { get; set; } = "";
    public string VideoPrivacy { get; set; } = "unlisted";
    public bool CreateAsDraft { get; set; }
    public int AuthorizedScopeVersion { get; set; }
}

internal sealed class MigrationState
{
    public Dictionary<string, PostState> Posts { get; set; } = [];
}

internal sealed class PostState
{
    public string BloggerPostId { get; set; } = "";
    public bool Complete { get; set; }
    public Dictionary<string, HostedMedia> Media { get; set; } = [];
}

internal sealed class HostedMedia
{
    public string Kind { get; set; } = "";
    public string Value { get; set; } = "";
    public bool Optimized { get; set; }
}

internal sealed record BloggerPostInfo(string Id, string Title, DateTimeOffset Published, string MigrationKey);
internal sealed record YouTubeVideoInfo(string Id, string Title, string Description, string OriginalFileName, long OriginalFileSize);
internal sealed record DriveImageInfo(string Id, string Name, long Size, string Md5Checksum);

internal sealed class MigrationReport
{
    public DateTime Started { get; } = DateTime.Now;
    public int Total { get; set; }
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public int Images { get; set; }
    public int Videos { get; set; }
    public List<string> Errors { get; } = [];
}
