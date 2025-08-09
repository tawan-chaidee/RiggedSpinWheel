// using System.IO;
// using Microsoft.AspNetCore.Mvc;

// [Route("/")]
// public class FrontendController : Controller
// {
//     // GET /
//     [HttpGet]
//     public IActionResult GetWheelController()
//     {
//         var filePath = Path.Combine(
//             Directory.GetCurrentDirectory(),
//             "wwwroot",
//             "wheel-controller.html"
//         );

//         if (!System.IO.File.Exists(filePath))
//         {
//             return NotFound("File not found");
//         }

//         var htmlContent = System.IO.File.ReadAllText(filePath);
//         return Content(htmlContent, "text/html");
//     }

//     // GET /join/{roomId}
//     [HttpGet("join/")]
//     public IActionResult GetWheelObserver(string roomId)
//     {
//         var filePath = Path.Combine(
//             Directory.GetCurrentDirectory(),
//             "wwwroot",
//             "wheel-observer.html"
//         );

//         if (!System.IO.File.Exists(filePath))
//         {
//             return NotFound("File not found");
//         }

//         var htmlContent = System.IO.File.ReadAllText(filePath);
//         return Content(htmlContent, "text/html");
//     }
// }

using System.IO;
using Microsoft.AspNetCore.Mvc;

public class FrontendController : Controller
{
    private readonly ISpinWheelRoomManager _roomManager;

    public FrontendController(ISpinWheelRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    // GET / - Create new room and redirect to controller page
    [HttpGet("/")]
    public IActionResult CreateRoomAndRedirect()
    {
        // Create new room and get room ID
        (string roomId, string token) = _roomManager.CreateRoom();
        
        // Redirect to controller page with room ID and token
        return Redirect($"/controller/{roomId}?token={Uri.EscapeDataString(token)}");
    }

    // GET /controller/{roomId} - Controller page for specific room
    [HttpGet("/controller/{roomId}")]
    public IActionResult GetWheelController(string roomId)
    {
        var filePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "wheel-controller.html"
        );

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("File not found");
        }

        return PhysicalFile(filePath, "text/html");
    }

    // GET /join/{roomId} - Observer page for specific room
    [HttpGet("/join/{roomId}")]
    public IActionResult GetWheelObserver(string roomId)
    {
        var filePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "wheel-observer.html"
        );

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("File not found");
        }

        return PhysicalFile(filePath, "text/html");
    }
}
