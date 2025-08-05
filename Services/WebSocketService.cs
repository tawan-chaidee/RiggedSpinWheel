using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

public class Room : Hub
{
    // connectionId -> (userName, roomId)
    private static readonly ConcurrentDictionary<string, (string userName, string roomId)> _users =
        new ConcurrentDictionary<string, (string, string)>();

    private readonly ISpinWheelRoomManager _roomManager;

    // Inject the room manager
    public Room(ISpinWheelRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public async Task Register(string userName, string roomId)
    {
        if (_roomManager.GetRoom(roomId) == null)
        {
            await Clients.Caller.SendAsync(
                "RegistrationFailed",
                $"Room '{roomId}' does not exist."
            );
            Console.WriteLine($"Registration failed for user {userName}. Room {roomId} not found.");
            return;
        }

        if (_users.TryAdd(Context.ConnectionId, (userName, roomId)))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            Console.WriteLine(
                $"User {userName} joined room {roomId} (Connection: {Context.ConnectionId})"
            );
            await Clients.Caller.SendAsync(
                "RegistrationSuccess",
                $"Successfully joined room {roomId}."
            );
        }
        else
        {
            await Clients.Caller.SendAsync(
                "RegistrationFailed",
                "Could not register. You might already be in a room."
            );
            Console.WriteLine(
                $"User {userName} registration failed for room {roomId} (Connection: {Context.ConnectionId}). Connection ID already exists."
            );
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_users.TryRemove(Context.ConnectionId, out var info))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, info.roomId);
            Console.WriteLine(
                $"User {info.userName} left room {info.roomId} (Connection: {Context.ConnectionId})"
            );
        }
        await base.OnDisconnectedAsync(exception);
    }
}
