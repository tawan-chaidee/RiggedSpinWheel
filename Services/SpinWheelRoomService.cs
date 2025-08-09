using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;


// Your interface stays the same
public interface ISpinWheelRoomManager
{
    (string roomId, string token) CreateRoom();
    bool RemoveRoom(string roomId);
    ISpinWheel GetRoom(string roomId);
    IEnumerable<ISpinWheel> GetAllRooms();
    Task<SpinResult> SpinWheelAsync(string roomId, List<string> future);
    Task<ISpinWheel> AddSegmentAsync(string roomId, string segmentName, int weight);
    Task<ISpinWheel> DeleteSegmentAsync(string roomId, string segmentName);
}

public class SpinWheelRoomManager : ISpinWheelRoomManager
{
    private readonly ConcurrentDictionary<string, ISpinWheel> rooms =
        new ConcurrentDictionary<string, ISpinWheel>();
    private readonly IHubContext<Room> _hubContext;

    // Your secret key for signing JWT tokens
    private readonly string jwtSecret = "X9p7V2kLm3BqRtZ8HwYd4NjSxCf1EuQa"; // Put in config for real apps

    public SpinWheelRoomManager(IHubContext<Room> hubContext)
    {
        _hubContext = hubContext;
    }

    // Updated CreateRoom returns both room ID and JWT token
    public (string roomId, string token) CreateRoom()
    {
        var roomId = Guid.NewGuid().ToString();
        var wheel = new SpinWheel(roomId);
        rooms.TryAdd(roomId, wheel);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(jwtSecret);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] 
            {
                new Claim("roomId", roomId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return (roomId, tokenString);
    }


    public async Task<ISpinWheel> AddSegmentAsync(string roomId, string segmentName, int weight)
    {
        var room = GetRoom(roomId);
        if (room == null)
        {
            Console.WriteLine($"Room '{roomId}' not found.");
            return null;
        }

        room.AddSegment(segmentName, weight);

        var json = JsonSerializer.Serialize(room);
        await _hubContext.Clients.Group(roomId).SendAsync("SegmentAdded", json);
        Console.WriteLine(
            $"Broadcasted segment addition in room {roomId}: {segmentName} with weight {weight}"
        );

        return room;
    }

    public bool RemoveRoom(string roomId) => rooms.TryRemove(roomId, out _);

    public ISpinWheel GetRoom(string roomId) =>
        rooms.TryGetValue(roomId, out var room) ? room : null;

    public IEnumerable<ISpinWheel> GetAllRooms() => rooms.Values;

    public async Task<SpinResult> SpinWheelAsync(string roomId, List<string> future)
    {
        var room = GetRoom(roomId) ?? throw new KeyNotFoundException($"Room '{roomId}' not found.");
        var spinResult = room.Spin(future);
        var json = JsonSerializer.Serialize(spinResult);
        await _hubContext.Clients.Group(roomId).SendAsync("SpinResult", json);
        Console.WriteLine($"Broadcasted spin in room {roomId}: {spinResult.Current}");
        return spinResult;
    }

    public async Task<ISpinWheel> DeleteSegmentAsync(string roomId, string segmentName)
    {
        var room = GetRoom(roomId);
        if (room == null)
        {
            Console.WriteLine($"Room '{roomId}' not found.");
            return null;
        }
        room.RemoveSegment(segmentName);

        var json = JsonSerializer.Serialize(room);
        await _hubContext.Clients.Group(roomId).SendAsync("SegmentDeleted", json);
        Console.WriteLine($"Broadcasted segment Deletion in room {roomId}: {segmentName}");

        return room;
    }
}
