using BlurHashSharp.ImageSharp;
using Microsoft.AspNetCore.Antiforgery;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapPost("/api/v1/blurhash", async (IFormFile file) =>
{
    if (file is { Length: > 0 })
    {
        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using (var image = await Image.LoadAsync<Rgba32>(stream))
            {
                image.Mutate(ctx => ctx.Resize(32, 32));
                using (var pixelStream = new MemoryStream())
                {
                    image.SaveAsBmp(pixelStream);
                    pixelStream.Position = 0;

                    // Validate dimensions
                    var xComponents = 4;
                    var yComponents = 4;

                    if (xComponents < 1 || xComponents > 9 || yComponents < 1 || yComponents > 9)
                    {
                        return Results.BadRequest("Invalid number of components.");
                    }

                    var blurHash = BlurHashEncoder.Encode(xComponents, yComponents, pixelStream);
                    return Results.Ok(new { Blurhash = blurHash });
                }
            }
        }
    }

    return Results.BadRequest("Invalid file.");
})
.WithName("BlurHash")
.WithMetadata(new IgnoreAntiforgeryMetadata());

app.Run();

public class IgnoreAntiforgeryMetadata : IAntiforgeryMetadata
{
    public bool RequiresValidation { get; }
}