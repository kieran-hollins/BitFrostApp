using BitFrost;
using System.Runtime.InteropServices;

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
LightingPatch patch = LightingPatch.Instance;
FXGenerator generator = FXGenerator.Instance;



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


app.Run();


