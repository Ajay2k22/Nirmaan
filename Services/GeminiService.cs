using System.Text;
using System.Text.Json;
using Nirmaan.Api.Interface;

namespace Nirmaan.Api.Services;

public class GeminiService : IGeminiService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<GeminiService> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<(bool success, int statusCode, string message, object? data)> DetectImageAsync(GeminiDetectDto dto)
    {
        try
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Photo))
                return (false, 400, "Photo is required", null);

            var apiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("YOUR"))
            {
                _logger.LogWarning("Gemini ApiKey not configured, using demo fallback");
                var demoData = new { severity = "high", reason = "Pothole detected on road - requires immediate attention" };
                return (true, 200, "Demo detection (ApiKey not configured)", demoData);
            }

            var base64Data = dto.Photo;
            var mimeType = "image/jpeg";
            if (base64Data.Contains(","))
            {
                var parts = base64Data.Split(",", 2);
                if (parts[0].Contains("image/"))
                {
                    try
                    {
                        var mimePart = parts[0].Split(";")[0].Split(":")[1];
                        mimeType = mimePart;
                    }
                    catch { }
                }
                base64Data = parts[1];
            }

            try
            {
                Convert.FromBase64String(base64Data);
            }
            catch
            {
                return (false, 400, "Invalid base64 photo format", null);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = "You are a civic complaint analyzer for Nirmaan app. Analyze this civic issue image. Is it pothole, garbage, broken streetlight, water leakage, etc? Return JSON only with 2 fields: severity (low, medium, high) and reason (short 1 sentence description). Example: {\"severity\": \"high\", \"reason\": \"Large pothole on main road causing accident risk\"}. No extra text." },
                                new { inline_data = new { mime_type = mimeType, data = base64Data } }
                            }
                        }
                    },
                    generationConfig = new { temperature = 0.4, maxOutputTokens = 200 }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API failed: {Status} - {Body}", response.StatusCode, responseString);

                    if ((int)response.StatusCode == 429)
                        return (true, 200, "Fallback detection (Rate limit)", new { severity = "medium", reason = "Civic issue detected - needs inspection" });

                    if ((int)response.StatusCode >= 500)
                        return (true, 200, "Fallback detection (Gemini service unavailable)", new { severity = "medium", reason = "Civic issue detected - needs inspection" });

                    return (true, 200, "Fallback detection (Gemini error)", new { severity = "medium", reason = "Civic issue detected - needs inspection" });
                }

                try
                {
                    using var doc = JsonDocument.Parse(responseString);
                    var text = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";

                    if (string.IsNullOrWhiteSpace(text))
                        return (true, 200, "Empty response from Gemini, using fallback", new { severity = "medium", reason = "Civic issue detected" });

                    try
                    {
                        var cleanText = text.Replace("```json", "").Replace("```", "").Trim();
                        using var innerDoc = JsonDocument.Parse(cleanText);
                        var severity = innerDoc.RootElement.TryGetProperty("severity", out var sevProp) ? sevProp.GetString() ?? "medium" : "medium";
                        var reason = innerDoc.RootElement.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() ?? "Civic issue detected" : text;

                        return (true, 200, "Image analyzed successfully", new { severity = severity.ToLower(), reason });
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogWarning(jsonEx, "Failed to parse Gemini JSON, fallback parsing. Raw:{Text}", text);
                        var severity = text.ToLower().Contains("high") ? "high" : text.ToLower().Contains("low") ? "low" : "medium";
                        return (true, 200, "Image analyzed with fallback parsing", new { severity, reason = text.Length > 200 ? text.Substring(0, 200) : text });
                    }
                }
                catch (Exception parseEx)
                {
                    _logger.LogError(parseEx, "Error parsing Gemini response: {Response}", responseString);
                    return (false, 500, "Error parsing Gemini response", new { error = parseEx.Message, severity = "medium", reason = "Civic issue detected" });
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error calling Gemini API");
                return (false, 503, "Failed to connect to Gemini service", new { error = httpEx.Message, severity = "medium", reason = "Issue detected - requires manual verification" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in DetectImage");
            return (false, 500, "Internal server error while detecting image", new { error = ex.Message, severity = "medium", reason = "Issue detected - requires manual verification" });
        }
    }
}
