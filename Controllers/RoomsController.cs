using Microsoft.AspNetCore.Authorization;
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
    /// Create a new room and return its ID and authentication token.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public IActionResult CreateRoom()
    {
        var (roomId, token) = _manager.CreateRoom();
        return CreatedAtAction(nameof(GetRoom), new { roomId }, new { roomId, token });
    }

    /// <summary>
    /// Delete a room by ID.
    /// </summary>
    [HttpDelete("{roomId}")]
    [Authorize]
    public IActionResult DeleteRoom(string roomId)
    {
        return _manager.RemoveRoom(roomId)
            ? NoContent()
            : NotFound(new { error = $"Room '{roomId}' not found." });
    }

    /// <summary>
    /// Get the state of a room.
    /// </summary>
    [HttpGet("{roomId}")]
    [Authorize]
    public IActionResult GetRoom(string roomId)
    {
        var room = _manager.GetRoom(roomId);
        return room != null ? Ok(room) : NotFound(new { error = $"Room '{roomId}' not found." });
    }

    /// <summary>
    /// List all active rooms.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetAllRooms()
    {
        var rooms = _manager.GetAllRooms();
        return Ok(rooms);
    }

    /// <summary>
    /// Spin the wheel and broadcast result.
    /// </summary>
    [HttpPost("{roomId}/spin")]
    [Authorize]
    public async Task<IActionResult> SpinWheel(string roomId, [FromBody] List<string> future)
    {
        try
        {
            var result = await _manager.SpinWheelAsync(roomId, future);
            return Ok(new { roomId, result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get spin history for a room.
    /// </summary>
    [HttpGet("{roomId}/history")]
    [Authorize]
    public IActionResult GetHistory(string roomId)
    {
        var room = _manager.GetRoom(roomId);
        return room != null
            ? Ok(room.History)
            : NotFound(new { error = $"Room '{roomId}' not found." });
    }

    /// <summary>
    /// Get current segments of a room.
    /// </summary>
    [HttpGet("{roomId}/segments")]
    [Authorize]
    public IActionResult GetSegments(string roomId)
    {
        var room = _manager.GetRoom(roomId);
        return room != null
            ? Ok(room.Segments)
            : NotFound(new { error = $"Room '{roomId}' not found." });
    }

    /// <summary>
    /// Add a segment and broadcast update.
    /// </summary>
    [HttpPost("{roomId}/segments")]
    [Authorize]
    public async Task<IActionResult> AddSegment(string roomId, [FromBody] AddSegmentRequest request)
    {
        try
        {
            var updatedRoom = await _manager.AddSegmentAsync(roomId, request.Name, request.Weight);
            return Ok(updatedRoom);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Add multiple segments and broadcast updates.
    /// </summary>
    [HttpPost("{roomId}/segments/batch")]
    [Authorize]
    public async Task<IActionResult> AddSegments(
        string roomId,
        [FromBody] List<AddSegmentRequest> segments
    )
    {
        try
        {
            var room = _manager.GetRoom(roomId);
            if (room == null)
            {
                return NotFound(new { error = $"Room '{roomId}' not found." });
            }

            foreach (var segment in segments)
            {
                await _manager.AddSegmentAsync(roomId, segment.Name, segment.Weight);
            }

            return CreatedAtAction(nameof(GetRoom), new { roomId }, segments);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Remove a segment and broadcast update.
    /// </summary>
    [HttpDelete("{roomId}/segments/{name}")]
    [Authorize]
    public async Task<IActionResult> RemoveSegment(string roomId, string name)
    {
        var result = await _manager.DeleteSegmentAsync(roomId, name);
        if (result == null)
        {
            return NotFound($"Room '{roomId}' not found or segment '{name}' does not exist.");
        }

        return Ok(result);
    }
}