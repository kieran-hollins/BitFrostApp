using BitFrost;
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
ArtNetController Controller = new("172.0.0.1", 0, LightingPatch.Instance);

app.MapGet("api/controller", () =>
{
    string jsonString = JsonSerializer.Serialize(Controller);
    return jsonString;
});

//app.MapGet("api/demo", () =>
//{
//    LightingPatch patch = LightingPatch.Instance;
//    patch.ClearAll();
//    FXGenerator generator = FXGenerator.Instance;
//    ArtNetController controller = new ArtNetController("127.0.0.1", 0, patch);
//    controller.Enable();
//    RGB rgb = new();
//    patch.AddLEDLineHorizontal(0, 0, 1, 10, rgb);
//    patch.AddLEDLineHorizontal(0, 1, 31, 10, rgb);
//    patch.AddLEDLineHorizontal(0, 2, 61, 10, rgb);
//    patch.AddLEDLineHorizontal(0, 3, 91, 10, rgb);
//    patch.AddLEDLineHorizontal(0, 4, 121, 10, rgb);

//    generator.WorkspaceHeight = 4; generator.WorkspaceWidth = 10;

//    generator.StaticColour(Utils.GetRandomColour());
//});

app.MapGet("api/demo/shader-test-fft", () =>
{
    LightingPatch patch = LightingPatch.Instance;
    patch.ClearAll();
    FXGenerator generator = FXGenerator.Instance;
    ArtNetController controller = new ArtNetController("127.0.0.1", 0, patch);
    controller.Enable();
    RGB rgb = new();
    patch.AddLEDLineHorizontal(0, 0, 1, 10, rgb);
    patch.AddLEDLineHorizontal(0, 1, 31, 10, rgb);
    patch.AddLEDLineHorizontal(0, 2, 61, 10, rgb);
    patch.AddLEDLineHorizontal(0, 3, 91, 10, rgb);
    patch.AddLEDLineHorizontal(0, 4, 121, 10, rgb);

    generator.WorkspaceHeight = 4; generator.WorkspaceWidth = 10;

    generator.ApplyMovementEffect("fft-glow");
});

app.MapGet("api/demo/flash", () =>
{
    LightingPatch patch = LightingPatch.Instance;
    patch.ClearAll();
    FXGenerator generator = FXGenerator.Instance;
    ArtNetController controller = new ArtNetController("127.0.0.1", 0, patch);
    controller.Enable();
    RGB rgb = new();
    patch.AddLEDLineHorizontal(0, 0, 1, 10, rgb);
    patch.AddLEDLineHorizontal(0, 1, 31, 10, rgb);
    patch.AddLEDLineHorizontal(0, 2, 61, 10, rgb);
    patch.AddLEDLineHorizontal(0, 3, 91, 10, rgb);
    patch.AddLEDLineHorizontal(0, 4, 121, 10, rgb);

    generator.WorkspaceHeight = 4; generator.WorkspaceWidth = 10;

    generator.SetColour(Utils.GetRandomColour());
    generator.ApplyMovementEffect("colour-flash");
});

app.MapGet("api/demo/bounce", () =>
{
    LightingPatch patch = LightingPatch.Instance;
    patch.ClearAll();
    FXGenerator generator = FXGenerator.Instance;
    ArtNetController controller = new ArtNetController("127.0.0.1", 0, patch);
    controller.Enable();
    RGB rgb = new();
    patch.AddLEDLineHorizontal(0, 0, 1, 10, rgb);
    patch.AddLEDLineHorizontal(0, 1, 31, 10, rgb);
    patch.AddLEDLineHorizontal(0, 2, 61, 10, rgb);
    patch.AddLEDLineHorizontal(0, 3, 91, 10, rgb);
    patch.AddLEDLineHorizontal(0, 4, 121, 10, rgb);

    generator.WorkspaceHeight = 4; generator.WorkspaceWidth = 10;

    generator.SetColour(Utils.GetRandomColour());
    generator.ApplyMovementEffect("horizontal-bounce");
});

app.MapGet("api/demo/s2l", () =>
{
    LightingPatch patch = LightingPatch.Instance;
    patch.ClearAll();
    FXGenerator generator = FXGenerator.Instance;
    ArtNetController controller = new ArtNetController("127.0.0.1", 0, patch);
    controller.Enable();
    RGB rgb = new();
    patch.AddLEDLineHorizontal(0, 0, 1, 10, rgb);
    patch.AddLEDLineHorizontal(0, 1, 31, 10, rgb);
    patch.AddLEDLineHorizontal(0, 2, 61, 10, rgb);
    patch.AddLEDLineHorizontal(0, 3, 91, 10, rgb);
    patch.AddLEDLineHorizontal(0, 4, 121, 10, rgb);

    generator.WorkspaceHeight = 4; generator.WorkspaceWidth = 10;

    generator.SetColour(Utils.GetRandomColour());
    generator.ApplyMovementEffect("beat-change");
});

//app.MapGet("api/demo/sendaudio", () =>
//{
//    FXGenerator generator = FXGenerator.Instance;
//    generator.SendTestAudio();
//});

app.MapGet("api/demo/rainbow-audio", () =>
{
    LightingPatch patch = LightingPatch.Instance;
    patch.ClearAll();
    FXGenerator generator = FXGenerator.Instance;
    ArtNetController controller = new ArtNetController("127.0.0.1", 0, patch);
    controller.Enable();
    RGB rgb = new();
    patch.AddLEDLineHorizontal(0, 0, 1, 10, rgb);
    patch.AddLEDLineHorizontal(0, 1, 31, 10, rgb);
    patch.AddLEDLineHorizontal(0, 2, 61, 10, rgb);
    patch.AddLEDLineHorizontal(0, 3, 91, 10, rgb);
    patch.AddLEDLineHorizontal(0, 4, 121, 10, rgb);

    generator.WorkspaceHeight = 4; generator.WorkspaceWidth = 10;
    generator.ApplyMovementEffect("rainbow-audio");

    return Results.Ok($"S2L effect enabled: \'rainbow audio\'");
});

app.MapPost("api/patch/LED", (int x, int y, int dmxAddress, string? type, HttpContext httpContext) =>
{
    var patch = LightingPatch.Instance;
    try
    {
        LED led;

        if (string.IsNullOrWhiteSpace(type))
        {
            led = LED.CreateRGBLED(dmxAddress);
        }
        else
        {
            switch (type.ToUpper())
            {
                case "RGB":
                    led = LED.CreateRGBLED(dmxAddress);
                    break;
                case "RGBW":
                    led = LED.CreateRGBWLED(dmxAddress);
                    break;
                case "GRB":
                    led = LED.CreateGRBLED(dmxAddress);
                    break;
                default:
                    return Results.BadRequest($"Unsupported LED type: {type}");
            }
        }

        patch.AddLED(x, y, led);
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

//app.MapPost("api/fx/static-colour", (string hexColour) =>
//{
//    var generator = FXGenerator.Instance;

//    byte[] channelValues = new byte[3];

//    channelValues = Utils.GetColourValuesFromHex(hexColour);
//    try
//    {
//        generator.StaticColour(channelValues);
//        return Results.Ok($"Static colour set. Hex Colour: {hexColour}");
//    }
//    catch (Exception e)
//    {
//        return Results.Problem(detail: e.Message);
//    }
//});

app.Run();