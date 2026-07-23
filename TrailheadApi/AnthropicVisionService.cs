using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrailheadApi;

public record EquipmentIdentification(
    string EquipmentId,
    string? Brand,
    string MachineName,
    string Confidence,
    string Note);

class AnthropicVisionService(HttpClient http, IConfiguration config, ILogger<AnthropicVisionService> logger)
{
    private static readonly string KnownIdsList = string.Join(", ", EquipmentCatalog.KnownIds);

    private const string JsonShape = """{"equipmentId": "...", "brand": "..." or null, "machineName": "...", "confidence": "high"|"medium"|"low", "note": "one short friendly sentence for the user"}""";

    private static readonly string Prompt = $"""
        You are helping identify gym equipment from a photo so a workout app can suggest exercises.
        Look at the image and identify the single piece of equipment it most prominently shows.

        Only choose "equipmentId" from this exact list: {KnownIdsList}.
        If nothing in the photo clearly matches one of these, or the photo doesn't show gym equipment,
        set "equipmentId" to "unknown".

        Also try to read any visible brand name or logo on the machine (e.g. Life Fitness, Hammer Strength,
        Precor, Technogym, Cybex, Matrix, Nautilus), and give a natural, human-readable machine name
        (e.g. "Seated Leg Press").

        Respond with ONLY a JSON object, no markdown fences and no other text, in exactly this shape:
        {JsonShape}
        """;

    public async Task<EquipmentIdentification> IdentifyAsync(byte[] imageBytes, string mediaType, CancellationToken ct)
    {
        var apiKey = config["Anthropic:ApiKey"] ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Anthropic API key is not configured. Set Anthropic:ApiKey (user-secrets) or the ANTHROPIC_API_KEY environment variable.");

        var model = config["Anthropic:Model"] ?? "claude-haiku-4-5-20251001";

        var payload = new
        {
            model,
            max_tokens = 300,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "image",
                            source = new
                            {
                                type = "base64",
                                media_type = mediaType,
                                data = Convert.ToBase64String(imageBytes)
                            }
                        },
                        new { type = "text", text = Prompt }
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Anthropic API call failed ({Status}): {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Vision provider returned {(int)response.StatusCode}.");
        }

        using var doc = JsonDocument.Parse(body);
        var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";

        return ParseIdentification(text);
    }

    private static EquipmentIdentification ParseIdentification(string text)
    {
        var jsonStart = text.IndexOf('{');
        var jsonEnd = text.LastIndexOf('}');
        if (jsonStart < 0 || jsonEnd < jsonStart)
            return new EquipmentIdentification(EquipmentCatalog.Unknown, null, "Unrecognized equipment", "low", "Couldn't make sense of that photo — try adding it manually.");

        var jsonSlice = text[jsonStart..(jsonEnd + 1)];
        try
        {
            var parsed = JsonSerializer.Deserialize<RawIdentification>(jsonSlice, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new JsonException("null result");

            var equipmentId = EquipmentCatalog.IsKnown(parsed.EquipmentId) ? parsed.EquipmentId! : EquipmentCatalog.Unknown;

            return new EquipmentIdentification(
                equipmentId,
                string.IsNullOrWhiteSpace(parsed.Brand) ? null : parsed.Brand,
                string.IsNullOrWhiteSpace(parsed.MachineName) ? "Unrecognized equipment" : parsed.MachineName,
                parsed.Confidence is "high" or "medium" or "low" ? parsed.Confidence : "low",
                string.IsNullOrWhiteSpace(parsed.Note) ? "Take a look and confirm this is right." : parsed.Note);
        }
        catch (JsonException)
        {
            return new EquipmentIdentification(EquipmentCatalog.Unknown, null, "Unrecognized equipment", "low", "Couldn't make sense of that photo — try adding it manually.");
        }
    }

    private record RawIdentification(
        [property: JsonPropertyName("equipmentId")] string? EquipmentId,
        [property: JsonPropertyName("brand")] string? Brand,
        [property: JsonPropertyName("machineName")] string? MachineName,
        [property: JsonPropertyName("confidence")] string? Confidence,
        [property: JsonPropertyName("note")] string? Note);
}
