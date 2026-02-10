using Microsoft.AspNetCore.Mvc;
using Scheduler.Api.DTOs;
using Scheduler.Api.Services;

namespace Scheduler.Api.Controllers;

[Route("/")]
public class CalendarController : ControllerBase
{
    private readonly CalendarService _calendarService;

    public CalendarController(CalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    [HttpGet("query")]
    public async Task<IActionResult> Query(AvailableSlotsRequest request) => Ok(await _calendarService.BuildCalendarAsync(request));
}
