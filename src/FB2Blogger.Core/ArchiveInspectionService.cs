namespace FB2Blogger;

public sealed record ArchiveInspectionResult(
    int PostCount,
    int ImageCount,
    int VideoCount,
    DateTimeOffset? EarliestPost,
    DateTimeOffset? LatestPost);

public sealed class ArchiveInspectionService
{
    readonly AppPathSet paths;
    readonly InspectionWorkspaceManager workspaceManager;

    public ArchiveInspectionService(AppPathSet paths)
        : this(paths, new FileSystemTemporaryDirectoryCleaner())
    {
    }

    public ArchiveInspectionService(AppPathSet paths, ITemporaryDirectoryCleaner directoryCleaner)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        workspaceManager = new(
            this.paths,
            directoryCleaner ?? throw new ArgumentNullException(nameof(directoryCleaner)));
    }

    /// <summary>
    /// Retries deletion of inspection workspaces left by an interrupted run or
    /// a previous cleanup failure. Only directories created by this service are touched.
    /// </summary>
    public void CleanupStaleWorkspaces(Action<string>? log = null)
        => workspaceManager.CleanupStaleWorkspaces(log);

    public async Task<ArchiveInspectionResult> InspectAsync(
        string archive,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(archive)) throw new FileNotFoundException(L.T("desktop_zip_missing"), archive);
        CleanupStaleWorkspaces(log);
        var workspace = workspaceManager.CreateWorkspace();
        var temporary = workspace.DirectoryPath;
        ArchiveInspectionResult? result = null;
        Exception? operationFailure = null;
        try
        {
            await Task.Run(() => FacebookArchiveExtractor.Extract(archive, temporary, cancellationToken), cancellationToken);
            var posts = await Task.Run(
                () => FacebookParser.Read(temporary, log ?? (_ => { }), cancellationToken),
                cancellationToken);
            var images = posts.Sum(post => post.Media.Count(media => !media.IsVideo));
            var videos = posts.Sum(post => post.Media.Count(media => media.IsVideo));
            result = new(
                posts.Count,
                images,
                videos,
                posts.Count == 0 ? null : posts.Min(post => post.Published),
                posts.Count == 0 ? null : posts.Max(post => post.Published));
        }
        catch (Exception error)
        {
            operationFailure = error;
        }

        Exception? cleanupFailure = null;
        try
        {
            workspaceManager.ReleaseAndDelete(workspace);
        }
        catch (Exception error)
        {
            cleanupFailure = error;
        }

        if (operationFailure is not null && cleanupFailure is not null)
            throw new AggregateException(L.T("temp_operation_and_cleanup_failed"), operationFailure, cleanupFailure);
        if (operationFailure is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(operationFailure).Throw();
        if (cleanupFailure is not null) throw cleanupFailure;
        return result!;
    }
}
