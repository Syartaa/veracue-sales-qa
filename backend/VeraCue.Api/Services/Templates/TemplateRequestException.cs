namespace VeraCue.Api.Services.Templates;

/// <summary>
/// A request that passed basic validation but conflicts with what's in the database,
/// e.g. a duplicate active template name (409) or a criterion id from another template (400).
/// The controller turns it into a field-level error response.
/// </summary>
public sealed class TemplateRequestException(int statusCode, string field, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Field { get; } = field;
}
