using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/rooms/{roomId}/state")]
public class WheelSpinStateController : ControllerBase
{
    private readonly ISpinWheelRoomManager _roomManager;

    public WheelSpinStateController(ISpinWheelRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    // GET api/rooms/{roomId}/state/history
    [HttpGet("history")]
    public IActionResult GetHistory(string roomId)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return NotFound($"Room '{roomId}' not found.");
        return Ok(room.History);
    }

    // GET api/rooms/{roomId}/state/segments
    [HttpGet("segments")]
    public IActionResult GetSegments(string roomId)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return NotFound($"Room '{roomId}' not found.");
        return Ok(room.Segments);
    }

    // POST api/rooms/{roomId}/state/spin
    [HttpPost("spin")]
    public IActionResult Spin(string roomId, [FromBody] List<string> forced = null)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return NotFound($"Room '{roomId}' not found.");
        try
        {
            var result = room.Spin(forced);
            return Ok(new { result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // POST api/rooms/{roomId}/state/segments
    [HttpPost("segments")]
    public IActionResult AddSegment(string roomId, [FromBody] Segment segment)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return NotFound($"Room '{roomId}' not found.");
        room.AddSegment(segment.Name, segment.Weight);
        return CreatedAtAction(nameof(GetSegments), new { roomId }, segment);
    }

    // POST api/rooms/{roomId}/state/segments/batch
    [HttpPost("segments/batch")]
    public IActionResult AddSegments(string roomId, [FromBody] List<Segment> segments)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return NotFound($"Room '{roomId}' not found.");

        foreach (var segment in segments)
        {
            room.AddSegment(segment.Name, segment.Weight);
        }

        return CreatedAtAction(nameof(GetSegments), new { roomId }, segments);
    }

    // DELETE api/rooms/{roomId}/state/segments/{name}
    [HttpDelete("segments/{name}")]
    public IActionResult RemoveSegment(string roomId, string name)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return NotFound($"Room '{roomId}' not found.");
        room.RemoveSegment(name);
        return NoContent();
    }


}
