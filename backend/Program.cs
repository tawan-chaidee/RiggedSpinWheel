using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

// Register the Swagger generator.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddSingleton<ISpinWheelRoomManager, SpinWheelRoomManager>();
builder.Services.AddControllers();
builder.Services.AddSignalR();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger(); 
    app.UseSwaggerUI(); 
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();
app.MapHub<Room>("/room"); // End point for Websocket connection
app.MapControllers();
app.Run();

