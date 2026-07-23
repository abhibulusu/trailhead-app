using TrailheadApi;

const long MaxPhotoBytes = 8 * 1024 * 1024; // 8 MB
var allowedMediaTypes = new HashSet<string> { "image/jpeg", "image/png", "image/webp" };

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<AnthropicVisionService>(client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com/");
    client.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Trailhead", policy =>
    {
        var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
        if (origins is { Length: > 0 })
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        else
            policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod(); // dev fallback: no origins configured
    });
});

var app = builder.Build();

app.UseCors("Trailhead");

app.MapGet("/", () => "Trailhead API is running.");

app.MapPost("/api/identify-equipment", async (HttpRequest request, AnthropicVisionService vision, ILogger<Program> logger, CancellationToken ct) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest(new { error = "Send the photo as multipart/form-data with a 'photo' field." });

    var form = await request.ReadFormAsync(ct);
    var photo = form.Files["photo"];
    if (photo is null || photo.Length == 0)
        return Results.BadRequest(new { error = "Missing 'photo' file." });

    if (photo.Length > MaxPhotoBytes)
        return Results.BadRequest(new { error = "Photo is too large. Please keep it under 8 MB." });

    var mediaType = photo.ContentType.ToLowerInvariant();
    if (!allowedMediaTypes.Contains(mediaType))
        return Results.BadRequest(new { error = "Unsupported image type. Use JPEG, PNG, or WebP." });

    using var stream = new MemoryStream();
    await photo.CopyToAsync(stream, ct);

    try
    {
        var result = await vision.IdentifyAsync(stream.ToArray(), mediaType, ct);
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogError(ex, "Equipment identification failed");
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
    }
});

app.Run();
