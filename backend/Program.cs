using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

// Register the Swagger generator.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddSingleton<ISpinWheelRoomManager, SpinWheelRoomManager>();
builder.Services.AddControllers();
builder.Services.AddSignalR();

// builder.Services.AddCors(options =>
// {
//     options.AddDefaultPolicy(policy =>
//     {
//         policy
//             .AllowAnyHeader()
//             .AllowAnyMethod()
//             .SetIsOriginAllowed(_ => true) // allow all origins
//             .AllowCredentials();
//     });
// });

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
// app.UseCors();
app.MapGet("/", () => "SignalR Room Service is running.");
app.MapHub<Room>("/room"); // single endpoint, multiple room support inside
app.MapControllers();
app.Run();

