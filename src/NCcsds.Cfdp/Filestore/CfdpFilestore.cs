namespace NCcsds.Cfdp.Filestore;

/// <summary>
/// CFDP filestore abstraction.
/// </summary>
public interface ICfdpFilestore
{
    /// <summary>
    /// Creates a file.
    /// </summary>
    FilestoreResponse CreateFile(string fileName);

    /// <summary>
    /// Deletes a file.
    /// </summary>
    FilestoreResponse DeleteFile(string fileName);

    /// <summary>
    /// Renames a file.
    /// </summary>
    FilestoreResponse RenameFile(string oldFileName, string newFileName);

    /// <summary>
    /// Appends to a file.
    /// </summary>
    FilestoreResponse AppendFile(string fileName, string sourceFileName);

    /// <summary>
    /// Replaces a file.
    /// </summary>
    FilestoreResponse ReplaceFile(string fileName, string sourceFileName);

    /// <summary>
    /// Creates a directory.
    /// </summary>
    FilestoreResponse CreateDirectory(string directoryName);

    /// <summary>
    /// Removes a directory.
    /// </summary>
    FilestoreResponse RemoveDirectory(string directoryName);

    /// <summary>
    /// Denies a file (removes if exists).
    /// </summary>
    FilestoreResponse DenyFile(string fileName);

    /// <summary>
    /// Denies a directory (removes if exists).
    /// </summary>
    FilestoreResponse DenyDirectory(string directoryName);

    /// <summary>
    /// Reads a file.
    /// </summary>
    byte[] ReadFile(string fileName);

    /// <summary>
    /// Writes a file.
    /// </summary>
    void WriteFile(string fileName, byte[] data);

    /// <summary>
    /// Checks if a file exists.
    /// </summary>
    bool FileExists(string fileName);

    /// <summary>
    /// Gets the size of a file.
    /// </summary>
    long GetFileSize(string fileName);
}

/// <summary>
/// Default filestore implementation using the local filesystem.
/// </summary>
public class LocalFilestore : ICfdpFilestore
{
    private readonly string _rootDirectory;

    /// <summary>
    /// Creates a new local filestore.
    /// </summary>
    public LocalFilestore(string rootDirectory)
    {
        _rootDirectory = rootDirectory;
        if (!Directory.Exists(_rootDirectory))
            Directory.CreateDirectory(_rootDirectory);
    }

    private string GetFullPath(string fileName)
    {
        // Sanitize path to prevent directory traversal
        var normalized = Path.GetFullPath(Path.Combine(_rootDirectory, fileName));
        if (!normalized.StartsWith(Path.GetFullPath(_rootDirectory)))
            throw new InvalidOperationException("Path traversal not allowed");
        return normalized;
    }

    /// <inheritdoc />
    public FilestoreResponse CreateFile(string fileName)
    {
        try
        {
            var path = GetFullPath(fileName);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.Create(path).Dispose();
            return FilestoreResponse.Successful;
        }
        catch
        {
            return FilestoreResponse.NotPerformed;
        }
    }

    /// <inheritdoc />
    public FilestoreResponse DeleteFile(string fileName)
    {
        try
        {
            var path = GetFullPath(fileName);
            if (!File.Exists(path))
                return FilestoreResponse.FileDoesNotExist;

            File.Delete(path);
            return FilestoreResponse.Successful;
        }
        catch
        {
            return FilestoreResponse.NotPerformed;
        }
    }

    /// <inheritdoc />
    public FilestoreResponse RenameFile(string oldFileName, string newFileName)
    {
        try
        {
            var oldPath = GetFullPath(oldFileName);
            var newPath = GetFullPath(newFileName);

            if (!File.Exists(oldPath))
                return FilestoreResponse.OldFileDoesNotExist;

            File.Move(oldPath, newPath);
            return FilestoreResponse.Successful;
        }
        catch
        {
            return FilestoreResponse.NotPerformed;
        }
    }

    /// <inheritdoc />
    public FilestoreResponse AppendFile(string fileName, string sourceFileName)
    {
        try
        {
            var filePath = GetFullPath(fileName);
            var sourcePath = GetFullPath(sourceFileName);

            if (!File.Exists(filePath))
                return FilestoreResponse.File1DoesNotExist;
            if (!File.Exists(sourcePath))
                return FilestoreResponse.File2DoesNotExist;

            var data = File.ReadAllBytes(sourcePath);
            using var stream = File.Open(filePath, FileMode.Append);
            stream.Write(data);

            return FilestoreResponse.Successful;
        }
        catch
        {
            return FilestoreResponse.NotPerformed;
        }
    }

    /// <inheritdoc />
    public FilestoreResponse ReplaceFile(string fileName, string sourceFileName)
    {
        try
        {
            var filePath = GetFullPath(fileName);
            var sourcePath = GetFullPath(sourceFileName);

            if (!File.Exists(sourcePath))
                return FilestoreResponse.File2DoesNotExist;

            File.Copy(sourcePath, filePath, true);
            return FilestoreResponse.Successful;
        }
        catch
        {
            return FilestoreResponse.NotPerformed;
        }
    }

    /// <inheritdoc />
    public FilestoreResponse CreateDirectory(string directoryName)
    {
        try
        {
            var path = GetFullPath(directoryName);
            if (Directory.Exists(path))
                return FilestoreResponse.DirectoryCannotBeCreated;

            Directory.CreateDirectory(path);
            return FilestoreResponse.Successful;
        }
        catch
        {
            return FilestoreResponse.NotPerformed;
        }
    }

    /// <inheritdoc />
    public FilestoreResponse RemoveDirectory(string directoryName)
    {
        try
        {
            var path = GetFullPath(directoryName);
            if (!Directory.Exists(path))
                return FilestoreResponse.DirectoryDoesNotExist;

            Directory.Delete(path, false);
            return FilestoreResponse.Successful;
        }
        catch
        {
            return FilestoreResponse.NotPerformed;
        }
    }

    /// <inheritdoc />
    public FilestoreResponse DenyFile(string fileName)
    {
        var path = GetFullPath(fileName);
        if (File.Exists(path))
            File.Delete(path);
        return FilestoreResponse.Successful;
    }

    /// <inheritdoc />
    public FilestoreResponse DenyDirectory(string directoryName)
    {
        var path = GetFullPath(directoryName);
        if (Directory.Exists(path))
            Directory.Delete(path, true);
        return FilestoreResponse.Successful;
    }

    /// <inheritdoc />
    public byte[] ReadFile(string fileName)
    {
        return File.ReadAllBytes(GetFullPath(fileName));
    }

    /// <inheritdoc />
    public void WriteFile(string fileName, byte[] data)
    {
        var path = GetFullPath(fileName);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllBytes(path, data);
    }

    /// <inheritdoc />
    public bool FileExists(string fileName)
    {
        return File.Exists(GetFullPath(fileName));
    }

    /// <inheritdoc />
    public long GetFileSize(string fileName)
    {
        return new FileInfo(GetFullPath(fileName)).Length;
    }
}

/// <summary>
/// Filestore response codes.
/// </summary>
public enum FilestoreResponse : byte
{
    /// <summary>
    /// Operation successful.
    /// </summary>
    Successful = 0,

    /// <summary>
    /// File does not exist.
    /// </summary>
    FileDoesNotExist = 1,

    /// <summary>
    /// Not allowed.
    /// </summary>
    NotAllowed = 2,

    /// <summary>
    /// Old file does not exist.
    /// </summary>
    OldFileDoesNotExist = 4,

    /// <summary>
    /// New file already exists.
    /// </summary>
    NewFileAlreadyExists = 5,

    /// <summary>
    /// Directory does not exist.
    /// </summary>
    DirectoryDoesNotExist = 6,

    /// <summary>
    /// Directory cannot be created.
    /// </summary>
    DirectoryCannotBeCreated = 7,

    /// <summary>
    /// File 1 does not exist.
    /// </summary>
    File1DoesNotExist = 8,

    /// <summary>
    /// File 2 does not exist.
    /// </summary>
    File2DoesNotExist = 9,

    /// <summary>
    /// Operation not performed.
    /// </summary>
    NotPerformed = 15
}
