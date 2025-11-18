using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using gurujiRide.Models;

namespace gurujiRide.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var model = new RideRequest
        {
            Pickup = string.Empty,
            Dropoff = string.Empty,
            Date = System.DateTime.Today,
            Time = System.DateTime.Now.TimeOfDay
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(RideRequest model)
    {
        // If validation fails, redisplay the form with the submitted values
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Mark that a submission occurred so the view can show a confirmation
        ViewData["Submitted"] = true;
        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
