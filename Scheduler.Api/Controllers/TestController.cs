using Microsoft.AspNetCore.Mvc;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Services;

namespace Scheduler.Api.Controllers;

[Route("/")]
public class TestController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly CalendarService _calendarService;

    public TestController(ApplicationDbContext dbContext, CalendarService calendarService)
    {
        _dbContext = dbContext;
        _calendarService = calendarService;
    }

    [HttpGet("doctors")]
    public async Task<IActionResult> Doctors() => Ok(_dbContext.Doctors.ToList());

    [HttpGet("specializations")]
    public async Task<IActionResult> Specializations() => Ok(_dbContext.Specializations.ToList());

    [HttpGet("appointments")]
    public async Task<IActionResult> Appointments() => Ok(_dbContext.Appointments.ToList());

    [HttpGet("schedules")]
    public async Task<IActionResult> Schedules() => Ok(_dbContext.Schedules.ToList());

    [HttpGet("scheduleSpecializations")]
    public async Task<IActionResult> scheduleSpecializations() => Ok(_dbContext.ScheduleSpecializations.ToList());

    [HttpGet("query")]
    public async Task<IActionResult> query(AvailableSlotsRequest request) => Ok(await _calendarService.Test(request));
}
