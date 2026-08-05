namespace FB2Blogger;

public sealed record BlogInfo(string Id, string Name)
{
    public override string ToString() => Name;
}

public sealed record MediaItem(string RelativePath, bool IsVideo);

public sealed record FacebookPost(
    string Key,
    string Title,
    string Text,
    DateTimeOffset Published,
    List<string> Labels,
    List<MediaItem> Media);

public sealed class AppSettings
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

public sealed class MigrationState
{
    public Dictionary<string, PostState> Posts { get; set; } = [];
}

public sealed class PostState
{
    public string BloggerPostId { get; set; } = "";
    public bool Complete { get; set; }
    public Dictionary<string, HostedMedia> Media { get; set; } = [];
}

public sealed class HostedMedia
{
    public string Kind { get; set; } = "";
    public string Value { get; set; } = "";
    public bool Optimized { get; set; }
}

public sealed record BloggerPostInfo(string Id, string Title, DateTimeOffset Published, string MigrationKey);
public sealed record YouTubeVideoInfo(string Id, string Title, string Description, string OriginalFileName, long OriginalFileSize);
public sealed record DriveImageInfo(string Id, string Name, long Size, string Md5Checksum);

public sealed class MigrationReport
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
