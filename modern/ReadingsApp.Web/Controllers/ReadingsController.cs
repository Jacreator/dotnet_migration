using Microsoft.AspNetCore.Mvc;
using ReadingsApp.Models;
using ReadingsApp.Services;

namespace ReadingsApp.Web.Controllers;

/// <summary>Thin controller: binds the request, calls the service, picks a view.</summary>
public class ReadingsController(IReadingService readings) : Controller
{
    // Feature 2: view readings, newest first, optional filter by meter.
    [HttpGet]
    public IActionResult Index(string meterId)
    {
        ViewData["MeterId"] = meterId;
        return View(readings.GetByMeter(meterId));
    }

    // Feature 1: submit a reading.
    [HttpGet]
    public IActionResult Create() => View(new ReadingInput { ReadingDate = DateTime.Today });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(ReadingInput input)
    {
        // ModelState only holds format errors here (for example letters in the value box).
        if (ModelState.IsValid)
        {
            foreach (var error in readings.Submit(input))
                ModelState.AddModelError(error.MemberNames.FirstOrDefault() ?? string.Empty, error.ErrorMessage);

            if (ModelState.IsValid)
            {
                TempData["Message"] = "Reading saved.";
                return RedirectToAction("Index", new { meterId = input.MeterId.Trim() });
            }
        }
        return View(input);
    }
}
