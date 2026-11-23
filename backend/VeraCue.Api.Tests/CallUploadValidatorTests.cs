using VeraCue.Api.Services.Calls;

namespace VeraCue.Api.Tests;

/// <summary>Rules an upload must meet before the recording is stored (see CallUploadValidator).</summary>
public class CallUploadValidatorTests
{
    private static readonly Guid TemplateId = Guid.NewGuid();

    private static Dictionary<string, string[]> Validate(
        string? fileName = "call-01.mp3",
        long size = 8_400_000,
        Guid? templateId = null,
        string? title = "Sales conversation #026",
        bool permission = true) =>
        CallUploadValidator.Validate(fileName, size, templateId ?? TemplateId, title, permission);

    [Theory]
    [InlineData("call.mp3")]
    [InlineData("call.WAV")]
    [InlineData("call.m4a")]
    public void Accepted_formats_pass(string fileName)
    {
        Assert.Empty(Validate(fileName: fileName));
    }

    [Theory]
    [InlineData("notes.txt")]
    [InlineData("video.mp4")]
    [InlineData("call")]
    public void Other_formats_are_rejected(string fileName)
    {
        Assert.Equal(["Upload an MP3, WAV or M4A file."], Validate(fileName: fileName)["file"]);
    }

    [Fact]
    public void Missing_or_empty_file_is_rejected()
    {
        Assert.True(Validate(fileName: null).ContainsKey("file"));
        Assert.True(Validate(size: 0).ContainsKey("file"));
    }

    [Fact]
    public void File_over_100_MB_is_rejected_and_exactly_100_MB_is_accepted()
    {
        Assert.True(Validate(size: CallUploadValidator.MaxFileBytes + 1).ContainsKey("file"));
        Assert.Empty(Validate(size: CallUploadValidator.MaxFileBytes));
    }

    [Fact]
    public void Template_is_required()
    {
        Assert.True(CallUploadValidator.Validate("call.mp3", 1000, null, "t", true).ContainsKey("templateId"));
        Assert.True(Validate(templateId: Guid.Empty).ContainsKey("templateId"));
    }

    [Fact]
    public void Permission_must_be_confirmed()
    {
        Assert.True(Validate(permission: false).ContainsKey("permissionConfirmed"));
    }

    [Fact]
    public void Title_longer_than_200_characters_is_rejected()
    {
        Assert.True(Validate(title: new string('a', 201)).ContainsKey("title"));
    }

    [Theory]
    [InlineData(null, "call-2026-11-21.mp3", "call-2026-11-21")]
    [InlineData("   ", "hargreaves.m4a", "hargreaves")]
    [InlineData("  Sales conversation #026  ", "x.mp3", "Sales conversation #026")]
    public void Blank_title_falls_back_to_the_file_name(string? title, string fileName, string expected)
    {
        Assert.Equal(expected, CallUploadValidator.ResolveTitle(title, fileName));
    }
}
