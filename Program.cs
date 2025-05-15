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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply CORS policy globally
app.UseCors();

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();
app.MapHub<Room>("/room"); // Endpoint for WebSocket connection

app.Run();
