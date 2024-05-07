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
LightingPatch patch = LightingPatch.Instance;

app.MapPost("api/patch/addLED", (int x, int y, int dmxAddress, string type, IPatchHelper patchHelper) => 
{
    
    
});
app.MapPost("api/patch/led", patch.AddRGBLED);


app.Run();


