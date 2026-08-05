namespace FB2Blogger;

/// <summary>
/// Deletes a temporary workspace. The abstraction keeps cleanup failures
/// deterministic and testable without weakening the production filesystem path.
/// </summary>
public interface ITemporaryDirectoryCleaner
{
    void DeleteRecursively(string path);
}

public sealed class FileSystemTemporaryDirectoryCleaner : ITemporaryDirectoryCleaner
{
    public void DeleteRecursively(string path) => Directory.Delete(path, true);
}
