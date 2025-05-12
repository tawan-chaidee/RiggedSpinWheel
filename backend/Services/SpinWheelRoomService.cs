
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR; 
using System.Text.Json; 

public interface ISpinWheelRoomManager
{
    string CreateRoom();
    bool RemoveRoom(string roomId);
    ISpinWheelState GetRoom(string roomId);
    IEnumerable<ISpinWheelState> GetAllRooms();
    Task<string> SpinWheelAndBroadcastAsync(string roomId, List<string> future);
}

public class SpinWheelRoomManager : ISpinWheelRoomManager
{
    private ConcurrentDictionary<string, ISpinWheelState> rooms = new ConcurrentDictionary<string, ISpinWheelState>();
    private readonly IHubContext<Room> _hubContext; // Inject Hub Context

    public SpinWheelRoomManager(IHubContext<Room> hubContext)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public string CreateRoom()
    {
        string roomId = Guid.NewGuid().ToString();
        var spinWheel = new SpinWheel(roomId);
        rooms.TryAdd(roomId, spinWheel);
        return roomId;
    }

    public bool RemoveRoom(string roomId)
    {
        return rooms.TryRemove(roomId, out _);
    }

    public ISpinWheelState GetRoom(string roomId)
    {
        rooms.TryGetValue(roomId, out var room);
        return room;
    }

    public IEnumerable<ISpinWheelState> GetAllRooms()
    {
        return rooms.Values;
    }

    public async Task<string> SpinWheelAndBroadcastAsync(string roomId, List<string> future)
    {
        ISpinWheelState room = GetRoom(roomId);
        if (room == null)
        {
            throw new KeyNotFoundException($"Room with ID '{roomId}' not found.");
        }

        string result = room.Spin(future);
        var payload = new { RoomId = roomId, Result = result };
        string jsonPayload = JsonSerializer.Serialize(payload); 

        // Broadcast the result to the specific SignalR group (room)
        await _hubContext.Clients.Group(roomId).SendAsync("SpinResult", jsonPayload);
        Console.WriteLine($"Spin result '{result}' broadcasted to room {roomId}");
        return result;
    }
}
