using Microsoft.AspNetCore.Mvc;

public class AddSegmentRequest
{
    public string Name { get; set; }
    public int Weight { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly ISpinWheelRoomManager _manager;

    public RoomsController(ISpinWheelRoomManager manager)
    {
        _manager = manager;
    }

    /// <summary>
    /// Create a new room and return its ID
    /// POST api/rooms
    /// </summary>
    [HttpPost]
    public IActionResult CreateRoom()
    {
        var roomId = _manager.CreateRoom();
        return CreatedAtAction(nameof(GetRoom), new { roomId }, new { roomId });
    }

    /// <summary>
    /// Remove an existing room
    /// DELETE api/rooms/{roomId}
    /// </summary>
    [HttpDelete("{roomId}")]
    public IActionResult DeleteRoom(string roomId)
    {
        return _manager.RemoveRoom(roomId)
            ? NoContent()
            : NotFound(new { error = $"Room '{roomId}' not found." });
    }

    /// <summary>
    /// Get a single room’s state
    /// GET api/rooms/{roomId}
    /// </summary>
    [HttpGet("{roomId}")]
    public IActionResult GetRoom(string roomId)
    {
        var room = _manager.GetRoom(roomId);
        return room != null
            ? Ok(room)
            : NotFound(new { error = $"Room '{roomId}' not found." });
    }

    /// <summary>
    /// List all rooms’ state
    /// GET api/rooms
    /// </summary>
    [HttpGet]
    public IActionResult GetAllRooms()
    {
        var rooms = _manager.GetAllRooms();
        return Ok(rooms);
    }

    /// <summary>
    /// Spin the wheel in a room and broadcast the result
    /// POST api/rooms/{roomId}/spin
    /// </summary>
    [HttpPost("{roomId}/spin")]
    public async Task<IActionResult> SpinWheel(string roomId, [FromBody] List<string> future)
    {
        try
        {
            var result = await _manager.SpinWheelAndBroadcastAsync(roomId, future);
            return Ok(new { roomId, result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Add a new segment to a room’s wheel and broadcast the update
    /// POST api/rooms/{roomId}/segments
    /// </summary>
    [HttpPost("{roomId}/segments")]
    public async Task<IActionResult> AddSegment(string roomId, [FromBody] AddSegmentRequest request)
    {
        try
        {
            var updatedRoom = await _manager.AddSegmentAndBroadcastAsync(roomId, request.Name, request.Weight);
            return Ok(updatedRoom); 
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // Batch support Todo
    [HttpPost("{roomId}/segments/batch")]
    public async Task<IActionResult> AddSegments(string roomId, [FromBody] List<AddSegmentRequest> segments)
    {
        try
        {
            // Get the room
            var room = _manager.GetRoom(roomId);
            if (room == null)
            {
                return NotFound(new { error = $"Room '{roomId}' not found." });
            }

            foreach (var segment in segments)
            {
                await _manager.AddSegmentAndBroadcastAsync(roomId, segment.Name, segment.Weight);
            }

            return CreatedAtAction(nameof(GetRoom), new { roomId }, segments);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }


}

