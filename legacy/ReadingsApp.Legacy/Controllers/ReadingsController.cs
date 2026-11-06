using System;
using System.Linq;
using System.Web.Mvc;
using ReadingsApp.Models;
using ReadingsApp.Services;

namespace ReadingsApp.Legacy.Controllers
{
    /// <summary>Thin controller: binds the request, calls the service, picks a view.</summary>
    public class ReadingsController : Controller
    {
        private readonly IReadingService _readings;

        public ReadingsController(IReadingService readings)
        {
            _readings = readings ?? throw new ArgumentNullException(nameof(readings));
        }

        // Feature 2: view readings, newest first, optional filter by meter.
        [HttpGet]
        public ActionResult Index(string meterId)
        {
            ViewBag.MeterId = meterId;
            return View(_readings.GetByMeter(meterId));
        }

        // Feature 1: submit a reading.
        [HttpGet]
        public ActionResult Create()
        {
            return View(new ReadingInput { ReadingDate = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ReadingInput input)
        {
            // ModelState only holds format errors here (for example letters in the value box).
            if (ModelState.IsValid)
            {
                foreach (var error in _readings.Submit(input))
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
}
