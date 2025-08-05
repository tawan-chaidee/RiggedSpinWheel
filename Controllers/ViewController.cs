using System.IO;
using Microsoft.AspNetCore.Mvc;

[Route("/")]
public class FrontendController : Controller
{
    // GET /
    [HttpGet]
    public IActionResult GetWheelController()
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

        var htmlContent = System.IO.File.ReadAllText(filePath);
        return Content(htmlContent, "text/html");
    }

    // GET /join/{roomId}
    [HttpGet("join/")]
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

        var htmlContent = System.IO.File.ReadAllText(filePath);
        return Content(htmlContent, "text/html");
    }

    // GET /getRoot
    [HttpGet("getRoot")]
    public IActionResult GetRootUrl()
    {
        var request = HttpContext.Request;
        var rootUrl = $"{request.Scheme}://{request.Host}/";

        return Content(rootUrl, "text/plain");
    }
}
