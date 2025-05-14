
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
    Task<SpinResult> SpinWheelAndBroadcastAsync(string roomId, List<string> future);
}

public class SpinWheelRoomManager : ISpinWheelRoomManager
{
    private ConcurrentDictionary<string, ISpinWheelState> rooms = new ConcurrentDictionary<string, ISpinWheelState>();
    private readonly IHubContext<Room> _hubContext;

    public SpinWheelRoomManager(IHubContext<Room> hubContext)
    {
        _hubContext = hubContext;
    }

    public string CreateRoom()
    {
        var id = Guid.NewGuid().ToString();
        var wheel = new SpinWheel(id);
        rooms.TryAdd(id, wheel);
        return id;
    }

    public bool RemoveRoom(string roomId)
        => rooms.TryRemove(roomId, out _);

    public ISpinWheelState GetRoom(string roomId)
        => rooms.TryGetValue(roomId, out var room) ? room : null;

    public IEnumerable<ISpinWheelState> GetAllRooms()
        => rooms.Values;

    public async Task<SpinResult> SpinWheelAndBroadcastAsync(string roomId, List<string> future)
    {
        var room = GetRoom(roomId) ?? throw new KeyNotFoundException($"Room '{roomId}' not found.");
        var spinResult = room.Spin(future);
        var json = JsonSerializer.Serialize(spinResult);
        await _hubContext.Clients.Group(roomId).SendAsync("SpinResult", json);
        Console.WriteLine($"Broadcasted spin in room {roomId}: {spinResult.Current}");
        return spinResult;
    }
}
