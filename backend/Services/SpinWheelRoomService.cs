
using System.Collections.Concurrent;

public interface ISpinWheelRoomManager
{
    string CreateRoom();
    bool RemoveRoom(string roomId);
    ISpinWheelState GetRoom(string roomId);
    IEnumerable<ISpinWheelState> GetAllRooms();
}


public class SpinWheelRoomManager : ISpinWheelRoomManager
{
    private ConcurrentDictionary<string, ISpinWheelState> rooms = new ConcurrentDictionary<string, ISpinWheelState>();



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
}
