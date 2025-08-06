using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

public interface ISpinWheelRoomManager
{
    string CreateRoom();
    bool RemoveRoom(string roomId);
    ISpinWheel GetRoom(string roomId);
    IEnumerable<ISpinWheel> GetAllRooms();
    Task<SpinResult> SpinWheelAsync(string roomId, List<string> future);
    Task<ISpinWheel> AddSegmentAsync(string roomId, string segmentName, int weight);
    Task<ISpinWheel> DeleteSegmentAsync(string roomId, string segmentName);
}


public class SpinWheelRoomManager : ISpinWheelRoomManager
{
    private ConcurrentDictionary<string, ISpinWheel> rooms =
        new ConcurrentDictionary<string, ISpinWheel>();
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

    public async Task<ISpinWheel> AddSegmentAsync(
        string roomId,
        string segmentName,
        int weight
    )
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
