using System.ComponentModel;

namespace StrangeSeeker;

public sealed record ReviewIssue(string File, int Line, string Severity, string Message);

/// <summary>
/// Code review tools for an AI agent. All file access is sandboxed to <see cref="RootDirectory"/>.
/// </summary>
public sealed class ReviewTools
{
    private static readonly string[] AllowedSeverities = ["info", "warning", "error", "critical"];

    private static readonly EnumerationOptions EnumerationOptions = new()
    {
        RecurseSubdirectories = true,
        // Don't follow symlinks or junctions, which could point outside the root.
        AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.Hidden | FileAttributes.System,
    };

    private readonly List<ReviewIssue> _issues = [];
    private readonly Lock _issuesLock = new();

    public ReviewTools(string rootDirectory)
    {
        RootDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootDirectory));

        if (!Directory.Exists(RootDirectory))
        {
            throw new DirectoryNotFoundException($"Review root directory not found: {RootDirectory}");
        }
    }

    public string RootDirectory { get; }

    public IReadOnlyList<ReviewIssue> Issues
    {
        get
        {
            lock (_issuesLock)
            {
                return [.. _issues];
            }
        }
    }

    [Description("Lists every file available for review, as paths relative to the review root.")]
    public IReadOnlyList<string> ListFiles() =>
        Directory.EnumerateFiles(RootDirectory, "*", EnumerationOptions)
            .Select(path => Path.GetRelativePath(RootDirectory, path).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToList();

    [Description("Reads the full text of a file. Use a relative path exactly as returned by ListFiles.")]
    public string ReadFile(
        [Description("Path of the file relative to the review root, e.g. 'Services/AuthService.cs'.")] string fileName)
    {
        if (!TryResolvePath(fileName, out var fullPath, out var error))
        {
            return error;
        }

        return File.ReadAllText(fullPath);
    }

    [Description("Records a code review issue found in a file.")]
    public string ReportIssue(
        [Description("Path of the file relative to the review root, as returned by ListFiles.")] string file,
        [Description("1-based line number where the issue occurs.")] int line,
        [Description("Severity of the issue: info, warning, error or critical.")] string severity,
        [Description("Clear explanation of the problem and how to fix it.")] string message)
    {
        if (!TryResolvePath(file, out _, out var error))
        {
            return error;
        }

        if (line < 1)
        {
            return "Error: line must be 1 or greater.";
        }

        var normalizedSeverity = severity.Trim().ToLowerInvariant();
        if (!AllowedSeverities.Contains(normalizedSeverity))
        {
            return $"Error: severity must be one of: {string.Join(", ", AllowedSeverities)}.";
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return "Error: message must not be empty.";
        }

        var relativePath = file.Replace('\\', '/');
        lock (_issuesLock)
        {
            _issues.Add(new ReviewIssue(relativePath, line, normalizedSeverity, message.Trim()));
        }

        return $"Issue recorded for {relativePath}:{line}.";
    }

    /// <summary>
    /// Resolves <paramref name="relativePath"/> against the root and rejects anything that escapes it,
    /// including <c>..</c> segments, absolute paths and symlinks.
    /// </summary>
    private bool TryResolvePath(string relativePath, out string fullPath, out string error)
    {
        fullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(relativePath) || relativePath.Contains('\0'))
        {
            error = "Error: a file path is required.";
            return false;
        }

        if (Path.IsPathRooted(relativePath))
        {
            error = "Error: access denied. Use a path relative to the review root.";
            return false;
        }

        // GetFullPath collapses "." and ".." segments, so "../../secrets.json" lands outside the root.
        var candidate = Path.GetFullPath(Path.Combine(RootDirectory, relativePath));
        if (!IsUnderRoot(candidate))
        {
            error = "Error: access denied. The path is outside the review root.";
            return false;
        }

        if (!File.Exists(candidate))
        {
            error = $"Error: file not found: {relativePath}";
            return false;
        }

        if (!IsFreeOfSymlinks(candidate))
        {
            error = "Error: access denied. Symbolic links are not allowed.";
            return false;
        }

        fullPath = candidate;
        error = string.Empty;
        return true;
    }

    private bool IsUnderRoot(string fullPath) =>
        fullPath.StartsWith(RootDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    // Checks the file and every directory between it and the root, so a linked
    // subdirectory can't be used to reach files outside the sandbox.
    private bool IsFreeOfSymlinks(string fullPath)
    {
        for (var path = fullPath; path != RootDirectory; path = Path.GetDirectoryName(path)!)
        {
            if (new FileInfo(path).Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                return false;
            }
        }

        return true;
    }
}
