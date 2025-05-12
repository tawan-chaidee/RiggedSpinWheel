using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/rooms/{roomId}/game")]
public class GameController : ControllerBase
{
    private readonly ISpinWheelRoomManager _roomManager;

    public GameController(ISpinWheelRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    [HttpPost("spin")]
    public async Task<IActionResult> SpinWheel(string roomId, [FromBody] List<string> future)
    {
        try
        {
            var result = await _roomManager.SpinWheelAndBroadcastAsync(roomId, future);
            return Ok(new { roomId, result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("history")]
    public IActionResult GetHistory(string roomId)
    {
        var room = _roomManager.GetRoom(roomId);
        return room != null
            ? Ok(room.History)
            : NotFound(new { error = $"Room '{roomId}' not found." });
    }

    [HttpGet("segments")]
    public IActionResult GetSegments(string roomId)
    {
        var room = _roomManager.GetRoom(roomId);
        return room != null
            ? Ok(room.Segments)
            : NotFound(new { error = $"Room '{roomId}' not found." });
    }

    [HttpPost("segments")]
    public IActionResult AddSegment(string roomId, [FromBody] Segment segment)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return NotFound(new { error = $"Room '{roomId}' not found." });
        
        room.AddSegment(segment.Name, segment.Weight);
        return CreatedAtAction(nameof(GetSegments), new { roomId }, segment);
    }

    [HttpPost("segments/batch")]
    public IActionResult AddSegments(string roomId, [FromBody] List<Segment> segments)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return NotFound(new { error = $"Room '{roomId}' not found." });

        foreach (var segment in segments)
        {
            room.AddSegment(segment.Name, segment.Weight);
        }
        return CreatedAtAction(nameof(GetSegments), new { roomId }, segments);
    }

    [HttpDelete("segments/{name}")]
    public IActionResult RemoveSegment(string roomId, string name)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return NotFound(new { error = $"Room '{roomId}' not found." });
        
        room.RemoveSegment(name);
        return NoContent();
    }
}