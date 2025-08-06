using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ISpinWheelRoomManager, SpinWheelRoomManager>();
builder.Services.AddControllers();
builder.Services.AddSignalR();

// Allow all origins for CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin() // Allow all origins
              .AllowAnyMethod()  // Allow all HTTP methods
              .AllowAnyHeader(); // Allow all headers
    });
});

builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();
app.MapHub<Room>("/room"); // Endpoint for WebSocket connection

app.Run();
