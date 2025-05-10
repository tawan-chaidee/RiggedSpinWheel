using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

    public class Room : Hub
    {
        // connectionId -> (userName, roomId)
        private static readonly ConcurrentDictionary<string, (string userName, string roomId)> _users
            = new ConcurrentDictionary<string, (string, string)>();

        // Register the user and add them to the specified room
        public async Task Register(string userName, string roomId)
        {
            _users[Context.ConnectionId] = (userName, roomId);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            Console.WriteLine($"User {userName} joined room {roomId} (Connection: {Context.ConnectionId})");
        }

        // Broadcast a JSON message to the specified room
        public Task Broadcast(string roomId, string jsonMessage)
        {
            return Clients.Group(roomId).SendAsync("ReceiveMessage", jsonMessage);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_users.TryRemove(Context.ConnectionId, out var info))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, info.roomId);
                Console.WriteLine($"User {info.userName} left room {info.roomId} (Connection: {Context.ConnectionId})");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }


