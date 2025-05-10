using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class Segment
{
    public string Name { get; set; }
    public int Weight { get; set; }
}

public interface ISpinWheelState
{
    List<string> History { get; }
    List<Segment> Segments { get; }
    string RoomId { get; }
    string Spin(List<string> future);
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

    public SpinWheel(string sessionId)
    {
        RoomId = sessionId;
    }

    public string Spin(List<string> future)
    {
        // If future is provided, enqueue the items as forced results
        if (future != null && future.Any())
        {
            // Check if each item in the future list exists in the segments
            foreach (var item in future)
            {
                if (!Segments.Any(s => s.Name == item))
                {
                    throw new ArgumentException($"Segment '{item}' not found in the wheel.");
                }
                forcedResults.Enqueue(item);
            }
        }

        string result;

        // If there are forced results, dequeue and use them
        if (forcedResults.Count > 0)
        {
            result = forcedResults.Dequeue();
        }
        else
        {
            // Otherwise, perform the regular random spin
            result = GetRandomSegment();
        }

        History.Add(result);
        return result;
    }


    public void AddSegment(string name, int weight)
    {
        Segments.Add(new Segment { Name = name, Weight = weight });
    }

    public void RemoveSegment(string name)
    {
        foreach (var segment in Segments)
        {
            if (segment.Name == name)
            {
                Segments.Remove(segment);
                return; 
            }
        }
    }


    private string GetRandomSegment()
    {
        if (Segments.Count == 0)
            throw new InvalidOperationException("No segments available.");

        int totalWeight = Segments.Sum(s => s.Weight);
        int randomNumber = random.Next(totalWeight);
        int cumulative = 0;

        foreach (var segment in Segments)
        {
            cumulative += segment.Weight;
            if (randomNumber < cumulative)
            {
                return segment.Name;
            }
        }

        return Segments.Last().Name;
    }
}

