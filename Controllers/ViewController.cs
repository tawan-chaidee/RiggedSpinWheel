using Microsoft.AspNetCore.Mvc;
using System.IO;

[Route("/")]
public class FrontendController : Controller
{
    // GET /
    [HttpGet]
    public IActionResult GetWheelController()
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "wheel-controller.html");

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("File not found");
        }

        var htmlContent = System.IO.File.ReadAllText(filePath);

        return Content(htmlContent, "text/html");
    }

    // GET /{roomId}
    [HttpGet("join/")]
    public IActionResult GetWheelObserver(string roomId)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "wheel-observer.html");

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("File not found");
        }

        var htmlContent = System.IO.File.ReadAllText(filePath);

        return Content(htmlContent, "text/html");
    }
}
