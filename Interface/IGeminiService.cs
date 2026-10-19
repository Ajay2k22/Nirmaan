

namespace Nirmaan.Api.Interface;

public class GeminiDetectDto
{
    public string Photo { get; set; } = "";
}

public interface IGeminiService
{
    Task<(bool success, int statusCode, string message, object? data)> DetectImageAsync(GeminiDetectDto dto);
}
