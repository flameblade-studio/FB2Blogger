using System.Net;
using System.Text;

namespace FB2Blogger;

public static class MigrationContent
{
    public static bool IsVideoPath(string path) => Path.GetExtension(path).ToLowerInvariant() is
        ".mp4" or ".mov" or ".m4v" or ".avi" or ".mkv" or ".webm";

    public static string ComposerCacheKey(string path)
    {
        var file = new FileInfo(path);
        return $"{Path.GetFullPath(path)}|{file.Length}|{file.LastWriteTimeUtc.Ticks}";
    }

    public static string BeginPostHtml(FacebookPost post)
    {
        var html = new StringBuilder($"<!-- FB2BLOGGER:{WebUtility.HtmlEncode(post.Key)} -->");
        if (!string.IsNullOrWhiteSpace(post.Text))
            html.Append("<div style=\"white-space:pre-wrap\">")
                .Append(WebUtility.HtmlEncode(post.Text))
                .Append("</div>");
        return html.ToString();
    }

    public static string ImageHtml(string url, string alternativeText) =>
        $"<p><img src=\"{WebUtility.HtmlEncode(url)}\" alt=\"{WebUtility.HtmlEncode(alternativeText)}\" style=\"max-width:100%;height:auto\"></p>";

    public static string VideoHtml(string videoId) =>
        $"<div style=\"margin:16px 0\"><iframe width=\"560\" height=\"315\" src=\"https://www.youtube.com/embed/{WebUtility.HtmlEncode(videoId)}\" title=\"YouTube video\" frameborder=\"0\" allowfullscreen></iframe></div>";

    public static string YouTubeDescription(FacebookPost post, MediaItem media)
    {
        var marker = $"\n\n[FB2Blogger:{post.Key}:{media.RelativePath.Replace('\\', '/')} ]";
        var maximumText = Math.Max(0, 5000 - marker.Length);
        var text = post.Text.Length <= maximumText ? post.Text : post.Text[..maximumText];
        if (text.Length > 0 && char.IsHighSurrogate(text[^1])) text = text[..^1];
        return text + marker;
    }

    public static string DriveIdFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return "";
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && parts[0] == "id") return Uri.UnescapeDataString(parts[1]);
        }
        return "";
    }

    public static string? ResolveMediaPath(string root, string relative)
    {
        var normalized = relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullRoot = Path.GetFullPath(root);
        var candidate = Path.GetFullPath(Path.Combine(fullRoot, normalized));
        var relation = Path.GetRelativePath(fullRoot, candidate);
        if (!Path.IsPathRooted(relation) && relation != ".." &&
            !relation.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
            !relation.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal) &&
            File.Exists(candidate))
            return candidate;

        var name = Path.GetFileName(relative);
        return string.IsNullOrEmpty(name)
            ? null
            : Directory.EnumerateFiles(fullRoot, name, SearchOption.AllDirectories).FirstOrDefault();
    }
}
