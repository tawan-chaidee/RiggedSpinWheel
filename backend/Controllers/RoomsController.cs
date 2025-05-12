using Microsoft.AspNetCore.Mvc;

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


    [HttpPost("{roomId}/spin")]
    public async Task<IActionResult> SpinWheel(string roomId, [FromBody] List<string> future)
    {
        try
        {
            string result = await _manager.SpinWheelAndBroadcastAsync(roomId, future);
            return Ok(new { roomId, result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

}
