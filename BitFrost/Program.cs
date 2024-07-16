using BitFrost;
using NAudio.SoundFont;
using System.Runtime.InteropServices;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

// Setup
ArtNetController Controller1 = new("192.168.0.10", 0, LightingPatch.Instance);
ArtNetController Controller2 = new("192.168.0.10", 1, LightingPatch.Instance);
ArtNetController Controller3 = new("192.168.0.10", 2, LightingPatch.Instance);
Controller1.Enable();
Controller2.Enable();
Controller3.Enable();

LightingPatch Patch = LightingPatch.Instance;
Patch.ClearAll();
RGB rgb = new();
Patch.AddRGBLEDLineHorizontal(0, 0, 1, 67);
Patch.AddRGBLEDLineHorizontal(0, 1, 202, 67);
Patch.AddRGBLEDLineHorizontal(0, 2, 403, 67);

FXGenerator Generator = FXGenerator.Instance;
Generator.WorkspaceHeight = 3; Generator.WorkspaceWidth = 67;




app.MapGet("api/demo/white", () =>
{
    Generator.ApplyMovementEffect("warm-white");
});

app.MapGet("api/demo/lava", () =>
{
    Generator.ApplyMovementEffect("lava-lamp");
});

app.MapGet("api/demo/fft", () =>
{
    Generator.ApplyMovementEffect("average");
});

app.MapGet("api/demo/level-meter", () =>
{
    Generator.ApplyMovementEffect("level-meter");
});

app.MapGet("api/demo/kaleidoscope", () =>
{
    Generator.ApplyMovementEffect("kaleidoscope");
});

app.MapGet("api/demo/kaleidoscope-audio", () =>
{
    Generator.ApplyMovementEffect("kaleidoscope-audio");
});

app.MapGet("api/demo/truchet", () =>
{
    Generator.ApplyMovementEffect("truchet");
});

app.MapGet("api/demo/spec-test", () =>
{
    Generator.ApplyMovementEffect("spectral-test");
});

app.MapGet("api/demo/sound-eclipse", () =>
{
    Generator.ApplyMovementEffect("sound-eclipse");
});


app.MapPost("api/patch/LED", (int x, int y, int dmxAddress, string? type, HttpContext httpContext) =>
{
    var patch = LightingPatch.Instance;
    try
    {
        LEDProfile profile = new RGB();

        if (!string.IsNullOrWhiteSpace(type))
        {
            switch (type.ToUpper())
            {
                case "RGB":
                    profile = new RGB();
                    break;
                case "RGBW":
                    profile = new RGBW();
                    break;
                case "GRB":
                    profile = new GRB();
                    break;
                default:
                    return Results.BadRequest($"Unsupported LED type: {type}. Creating RGB instead.");
            }
        }

        patch.AddLED(x, y, dmxAddress, profile);
        return Results.Ok($"LED of type {type} added at ({x}, {y}) with starting DMX address {dmxAddress}");
        
    }
    catch (Exception e) 
    {
        return Results.Problem(detail: e.Message);
    }
});

app.MapDelete("api/patch/LED", (int x, int y) =>
{
    var patch = LightingPatch.Instance;
    
    try
    {
        patch.RemoveLED(x, y);
        return Results.Ok($"LED at position ({x}, {y}) has been removed successfully.");
    }
    catch (Exception e)
    {
        return Results.Problem(detail: e.Message);
    }
});

app.Run();