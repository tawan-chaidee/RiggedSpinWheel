using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

public class Segment
{
    public string Name { get; set; }
    public int Weight { get; set; }
}

public class SpinResult
{
    public string Current { get; set; }
    public string Previous { get; set; }
    public List<Segment> NewState { get; set; }
    public List<string> History { get; set; }
}

public interface ISpinWheelState
{
    List<string> History { get; }
    List<Segment> Segments { get; }
    string RoomId { get; }
    SpinResult Spin(List<string> future);
    void AddSegment(string name, int weight);
    void RemoveSegment(string name);
}

public class SpinWheel : ISpinWheelState
{
    private static readonly Random random = new Random();
    private Queue<string> forcedResults = new Queue<string>();

    public List<string> History { get; } = new List<string>();
    public List<Segment> Segments { get; } = new List<Segment>();
    public string RoomId { get; }

    public SpinWheel(string roomId)
    {
        RoomId = roomId;
    }

    public SpinResult Spin(List<string> future)
    {
        // Enqueue forced future results
        if (future != null && future.Any())
        {
            foreach (var name in future)
            {
                if (!Segments.Any(s => s.Name == name))
                    throw new ArgumentException($"Segment '{name}' not found on wheel.");
                forcedResults.Enqueue(name);
            }
        }

        if (Segments.Count == 0)
            throw new InvalidOperationException("No segments to spin.");

        var previous = History.LastOrDefault();
        string current = forcedResults.Count > 0 ? forcedResults.Dequeue() : GetRandomSegment();
        Segments.RemoveAll(s => s.Name == current);
        History.Add(current);

        return new SpinResult
        {
            Current = current,
            Previous = previous,
            NewState = Segments.ToList(), 
            History = History.ToList(),
        };
    }

    public void AddSegment(string name, int weight)
    {
        Segments.Add(new Segment { Name = name, Weight = weight });
    }

    public void RemoveSegment(string name)
    {
        Segments.RemoveAll(s => s.Name == name);
    }

    private string GetRandomSegment()
    {
        int total = Segments.Sum(s => s.Weight);
        int pick = random.Next(total);
        int acc = 0;
        foreach (var seg in Segments)
        {
            acc += seg.Weight;
            if (pick < acc)
                return seg.Name;
        }
        return Segments.Last().Name;
    }
}
