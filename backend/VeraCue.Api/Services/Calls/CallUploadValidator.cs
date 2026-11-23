namespace VeraCue.Api.Services.Calls;

/// <summary>
/// Checks an upload before anything is stored. Error keys match the form fields
/// ("file", "templateId", "title", "permissionConfirmed") so the page can show them in place.
/// </summary>
public static class CallUploadValidator
{
    public const long MaxFileBytes = 100L * 1024 * 1024; // 100 MB (UI spec placeholder, agreed)
    public const int TitleMaxLength = 200;

    /// <summary>Accepted formats: extension -> content type stored with the call.</summary>
    public static readonly IReadOnlyDictionary<string, string> AllowedExtensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".mp3"] = "audio/mpeg",
            [".wav"] = "audio/wav",
            [".m4a"] = "audio/mp4",
        };

    public static Dictionary<string, string[]> Validate(
        string? fileName, long fileSize, Guid? templateId, string? title, bool permissionConfirmed)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(fileName) || fileSize == 0)
            errors["file"] = ["Choose a call recording to upload."];
        else if (!AllowedExtensions.ContainsKey(Path.GetExtension(fileName)))
            errors["file"] = ["Upload an MP3, WAV or M4A file."];
        else if (fileSize > MaxFileBytes)
            errors["file"] = ["The recording is larger than 100 MB."];

        if (templateId is null || templateId == Guid.Empty)
            errors["templateId"] = ["Choose a QA template."];

        if (title?.Trim().Length > TitleMaxLength)
            errors["title"] = [$"The call title can be at most {TitleMaxLength} characters."];

        if (!permissionConfirmed)
            errors["permissionConfirmed"] =
                ["Confirm that you're permitted to process this recording with third-party AI services."];

        return errors;
    }

    /// <summary>The title to save: the one typed, or the file name without its extension.</summary>
    public static string ResolveTitle(string? title, string fileName) =>
        string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(fileName) : title.Trim();
}
