// Program.cs has not been touched yet. API will be built when other functionality is completed.


using BitFrost;

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

// Adding patch manually for testing purposes
LightingPatch patch = new LightingPatch();

int x = 0, y = 0, startAddress = 1, length = 6;
RGB type = new();
patch.AddLEDLineHorizontal(x, y, startAddress, length, type);

x = 0;
y = 2;
startAddress = 19;
length = 6;
patch.AddLEDLineHorizontal(x, y, startAddress, length, type);

app.MapGet("api/led/location", patch.getLEDLocation);
app.MapPost("api/led", patch.AddRGBLED);

app.Run();


