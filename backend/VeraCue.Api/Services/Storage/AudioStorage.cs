namespace VeraCue.Api.Services.Storage;

/// <summary>
/// Private storage for call recordings on a persistent disk.
/// Production: a Render Disk mounted at /var/data (not reachable from the internet).
/// Local: the git-ignored "local-audio" folder in the repo root.
/// Recordings never go into PostgreSQL; Calls.AudioStorageKey stores the relative path
/// returned here, and only the API reads the files (for Deepgram and the audio player).
/// </summary>
public sealed class AudioStorage
{
    private readonly string _root;

    public AudioStorage(IConfiguration config, IHostEnvironment env)
    {
        var configured = config["AUDIO_STORAGE_PATH"];
        _root = Path.GetFullPath(configured ?? (env.IsDevelopment()
            ? Path.Combine(env.ContentRootPath, "..", "..", "local-audio")
            : "/var/data/recordings"));
    }

    /// <summary>Relative storage key for a call's recording, e.g. "calls/{callId}/call-01.mp3".</summary>
    public string KeyForCall(Guid callId, string originalFileName)
    {
        var safeName = string.Concat(Path.GetFileName(originalFileName)
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_' ? ch : '-'));
        return $"calls/{callId}/{safeName}";
    }

    /// <summary>
    /// Saves the stream under the key. Written to a temporary file first and then renamed,
    /// so a crash mid-upload never leaves a half-written recording under the real name.
    /// </summary>
    public async Task SaveAsync(string key, Stream content, CancellationToken ct)
    {
        var path = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var tempPath = path + ".uploading";
        await using (var file = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None,
                         bufferSize: 81920, useAsync: true))
        {
            await content.CopyToAsync(file, ct);
        }
        File.Move(tempPath, path, overwrite: true);
    }

    /// <summary>Opens a recording for reading (e.g. to stream it to Deepgram).</summary>
    public Stream OpenRead(string key) =>
        new FileStream(ResolvePath(key), FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 81920, useAsync: true);

    /// <summary>Absolute path, used to stream the file to the browser with seeking support.</summary>
    public string GetFullPath(string key) => ResolvePath(key);

    public bool Exists(string key) => File.Exists(ResolvePath(key));

    public Task DeleteAsync(string key, CancellationToken ct)
    {
        var path = ResolvePath(key);
        if (File.Exists(path)) File.Delete(path);

        var folder = Path.GetDirectoryName(path)!;
        if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
            Directory.Delete(folder);

        return Task.CompletedTask;
    }

    /// <summary>Health check: proves the disk is mounted and writable by the app's user.</summary>
    public async Task CheckWritableAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(_root);
        var probe = Path.Combine(_root, $".health-{Guid.NewGuid():N}");
        await File.WriteAllTextAsync(probe, "ok", ct);
        File.Delete(probe);
    }

    /// <summary>Turns a key into a full path and refuses anything that would escape the storage folder.</summary>
    private string ResolvePath(string key)
    {
        var path = Path.GetFullPath(Path.Combine(_root, key));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage key.");
        return path;
    }
}
