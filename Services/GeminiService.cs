using System.Text;
using System.Text.Json;
using Nirmaan.Api.Interface;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Nirmaan.Api.Services;

public class GeminiService : IGeminiService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GeminiService> _logger;

    // LOW TOKEN MODELS - Same for both methods
    private readonly string[] LowTokenModels = new[]
    {
        "gemini-2.5-flash-lite",
        "gemini-3.5-flash-lite",
        "gemini-3.1-flash-lite",
        "gemini-2.5-flash",
        "gemini-3.5-flash"
    };

    public GeminiService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<GeminiService> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private string CleanAndFixBase64(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        if (input.Contains(",")) input = input.Split(",", 2)[1];
        input = input.Trim().Replace("\n", "").Replace("\r", "").Replace(" ", "").Replace("\"", "").Replace("'", "");
        int mod = input.Length % 4;
        if (mod > 0) input = input.PadRight(input.Length + (4 - mod), '=');
        return input;
    }

    private string CompressBase64(string base64, int maxSize = 768)
    {
        try
        {
            var cleaned = CleanAndFixBase64(base64);
            if (string.IsNullOrEmpty(cleaned)) return "";
            var bytes = Convert.FromBase64String(cleaned);
            using var image = Image.Load(bytes);
            if (image.Width > maxSize || image.Height > maxSize)
                image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(maxSize, maxSize), Mode = ResizeMode.Max }));
            using var ms = new MemoryStream();
            image.Save(ms, new JpegEncoder { Quality = 65 });
            return Convert.ToBase64String(ms.ToArray());
        }
        catch { var c = CleanAndFixBase64(base64); try { Convert.FromBase64String(c); return c; } catch { return ""; } }
    }

    public async Task<(bool success, int statusCode, string message, object? data)> CompareImagesAsync(string originalBase64, string proofBase64)
    {
        try
        {
            var apiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("YOUR"))
                return (true, 200, "Demo", new { status = "RESOLVED", mssg = "Demo mode", reason = "ApiKey missing", isFake = false, confidence = "low" });

            var origB64 = CompressBase64(originalBase64, 768);
            var proofB64 = CompressBase64(proofBase64, 768);
            if (string.IsNullOrEmpty(origB64) || string.IsNullOrEmpty(proofB64))
                return (false, 400, "Invalid base64", null);

            var client = _httpClientFactory.CreateClient();
            string? lastError = null;

            foreach (var model in LowTokenModels)
            {
                try
                {
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                    var systemPrompt = @"Forensic civic verifier. I1=original complaint, I2=proof fix.
Check: 1) I2 fake/AI? 2) Same location? Compare background. 3) Pothole fixed in I2?
Rules: If fake or different location or not fixed => REJECTED else RESOLVED.
Return JSON only: {""status"":""RESOLVED""/""REJECTED"",""mssg"":""short"",""isFake"":bool,""reason"":""why"",""confidence"":""high/medium/low""}";

                    var requestBody = new
                    {
                        contents = new[] { new { parts = new object[] { new { text = systemPrompt }, new { inline_data = new { mime_type = "image/jpeg", data = origB64 } }, new { inline_data = new { mime_type = "image/jpeg", data = proofB64 } } } } },
                        generationConfig = new { temperature = 0.1, maxOutputTokens = 300, response_mime_type = "application/json", thinkingConfig = new { thinkingBudget = 0 } }
                    };

                    var response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
                    var responseString = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        lastError = responseString;
                        if (responseString.Contains("NOT_FOUND") || responseString.Contains("not found") || responseString.Contains("is no longer available")) continue;
                        continue;
                    }

                    using var doc = JsonDocument.Parse(responseString);
                    if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0) continue;
                    var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
                    var cleanText = text.Replace("```json", "").Replace("```", "").Trim();
                    using var innerDoc = JsonDocument.Parse(cleanText);
                    var root = innerDoc.RootElement;
                    var status = root.TryGetProperty("status", out var s) ? s.GetString()?.ToUpper() ?? "REJECTED" : "REJECTED";
                    var mssg = root.TryGetProperty("mssg", out var m) ? m.GetString() : cleanText;
                    var isFake = root.TryGetProperty("isFake", out var f) ? f.GetBoolean() : false;
                    var reason = root.TryGetProperty("reason", out var r) ? r.GetString() : mssg;
                    var confidence = root.TryGetProperty("confidence", out var c) ? c.GetString() : "high";
                    if (status != "RESOLVED" && status != "REJECTED") status = "REJECTED";
                    return (true, 200, "Compared", new { status, mssg, reason, isFake, confidence, modelUsed = model, tokenOptimized = true });
                }
                catch (Exception ex) { lastError = ex.Message; continue; }
            }
            return (false, 500, "All Gemini models failed", new { error = lastError });
        }
        catch (Exception ex) { return (false, 500, "Error", new { error = ex.Message }); }
    }

    public async Task<(bool success, int statusCode, string message, object? data)> DetectImageAsync(GeminiDetectDto dto)
    {
        try
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Photo))
                return (false, 400, "Photo is required", null);

            var apiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("YOUR"))
                return (true, 200, "Demo", new
                {
                    severity = "HIGH",
                    category = "ROADS",
                    reason = "Pothole detected - requires immediate attention",
                    message = "The road surface has multiple potholes filled with water, which pose a safety hazard to vehicles and pedestrians."
                });

            var base64Data = CompressBase64(dto.Photo, 512);
            if (string.IsNullOrEmpty(base64Data))
                return (false, 400, "Invalid base64 photo format", null);

            var client = _httpClientFactory.CreateClient();
            string? lastError = null;

            foreach (var model in LowTokenModels)
            {
                try
                {
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                    var requestBody = new
                    {
                        contents = new[]
                        {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = @"You are civic complaint analyzer for Indian municipal corporation.
Analyze the image carefully and classify.

Rules:
1. severity MUST be one of: HIGH, LOW, MEDIUM (UPPERCASE ONLY)
2. category MUST be one of: ROADS, GARBAGE_WASTE, DRAINAGE_SEWAGE, STREET_LIGHT, WATER_SUPPLY, PUBLIC_TOILET, ENCROACHMENT, PARK_GARDEN, TRAFFIC_SIGNAL, STRAY_ANIMALS, NOISE_POLLUTION, OTHER
   - ROADS = pothole, road damage, broken road
   - GARBAGE_WASTE = garbage, waste, overflowing dustbin
   - DRAINAGE_SEWAGE = drainage, sewage, manhole, waterlogging
   - STREET_LIGHT = street light, lamp post
   - WATER_SUPPLY = water leak, pipeline
   - PUBLIC_TOILET = toilet
   - ENCROACHMENT = illegal construction, footpath blocked
   - PARK_GARDEN = park, garden, tree
   - TRAFFIC_SIGNAL = traffic light, signal
   - STRAY_ANIMALS = dogs, cattle, animals
   - NOISE_POLLUTION = loudspeaker, noise
   - OTHER = anything else
3. Return JSON ONLY, no markdown, no explanation, in this exact format:
{""severity"":""HIGH"",""category"":""ROADS"",""reason"":""1 sentence short reason"",""message"":""Detailed description of issue in 1-2 sentences for complaint title""}

Examples:
{""severity"":""HIGH"",""category"":""ROADS"",""reason"":""Multiple potholes pose safety risk"",""message"":""The road surface has multiple potholes filled with water, which pose a safety hazard to vehicles and pedestrians.""}
{""severity"":""LOW"",""category"":""GARBAGE_WASTE"",""reason"":""Garbage overflow causing foul smell"",""message"":""Garbage bags are overflowing on roadside causing foul smell and health hazard.""}
{""severity"":""HIGH"",""category"":""DRAINAGE_SEWAGE"",""reason"":""Sewage overflow on street"",""message"":""Drainage water is overflowing and sewage is leaking onto the street.""}
" },
                                new { inline_data = new { mime_type = "image/jpeg", data = base64Data } }
                            }
                        }
                    },
                        generationConfig = new { temperature = 0.2, maxOutputTokens = 250, response_mime_type = "application/json", thinkingConfig = new { thinkingBudget = 0 } }
                    };

                    var response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
                    var responseString = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        lastError = responseString;
                        _logger.LogWarning("Detect Model {Model} failed {Status}: {Body}", model, response.StatusCode, responseString.Length > 200 ? responseString.Substring(0, 200) : responseString);
                        if (responseString.Contains("NOT_FOUND") || responseString.Contains("not found") || responseString.Contains("is no longer available"))
                            continue;
                        continue;
                    }

                    using var doc = JsonDocument.Parse(responseString);
                    if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                    {
                        lastError = "No candidates";
                        continue;
                    }

                    var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
                    var cleanText = text.Replace("```json", "").Replace("```", "").Trim();

                    _logger.LogInformation("Detect Gemini {Model} success: {Text}", model, cleanText);

                    using var innerDoc = JsonDocument.Parse(cleanText);
                    var severityRaw = innerDoc.RootElement.TryGetProperty("severity", out var sevProp) ? sevProp.GetString() ?? "MEDIUM" : "MEDIUM";
                    var categoryRaw = innerDoc.RootElement.TryGetProperty("category", out var catProp) ? catProp.GetString() ?? "OTHER" : "OTHER";
                    var reason = innerDoc.RootElement.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() ?? "Civic issue detected" : cleanText;
                    var message = innerDoc.RootElement.TryGetProperty("message", out var msgProp) ? msgProp.GetString() ?? reason : reason;

                    // Enforce allowed values
                    var severity = severityRaw.ToUpper().Trim();
                    if (severity != "HIGH" && severity != "LOW" && severity != "MEDIUM") severity = "MEDIUM";

                    var allowedCategories = new[] { "ROADS", "GARBAGE_WASTE", "DRAINAGE_SEWAGE", "STREET_LIGHT", "WATER_SUPPLY", "PUBLIC_TOILET", "ENCROACHMENT", "PARK_GARDEN", "TRAFFIC_SIGNAL", "STRAY_ANIMALS", "NOISE_POLLUTION", "OTHER" };
                    var category = categoryRaw.ToUpper().Trim();
                    if (!allowedCategories.Contains(category)) category = "OTHER";

                    return (true, 200, message, new { severity = severity, category = category, reason = reason, message = message, modelUsed = model, tokenOptimized = true });
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    _logger.LogWarning(ex, "Detect Model {Model} exception", model);
                    continue;
                }
            }

            _logger.LogError("All detect models failed: {Error}", lastError);
            return (true, 200, "Fallback detection (All models failed)", new { severity = "MEDIUM", category = "OTHER", reason = "Civic issue detected - needs inspection", message = "Civic issue detected - needs inspection", error = lastError });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DetectImage error");
            return (true, 200, "Fallback detection", new { severity = "MEDIUM", category = "OTHER", reason = "Civic issue detected", message = "Civic issue detected" });
        }
    }
}
