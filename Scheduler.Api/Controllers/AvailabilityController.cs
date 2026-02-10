using Microsoft.AspNetCore.Mvc;
using Scheduler.Api.DTOs;
using Scheduler.Api.Services;

namespace Scheduler.Api.Controllers;

[Route("/api/availability")]
public class AvailabilityController : ControllerBase
{
    private readonly CalendarService _calendarService;

    public AvailabilityController(CalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    [HttpGet]
    public async Task<IActionResult> Query(AvailableSlotsRequest request) => Ok(await _calendarService.BuildCalendarAsync(request));
}
