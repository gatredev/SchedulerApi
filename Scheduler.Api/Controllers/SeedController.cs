using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.Models;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SeedController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize()
    {
        _context.Appointments.RemoveRange(_context.Appointments);
        _context.Doctors.RemoveRange(_context.Doctors);
        _context.Specializations.RemoveRange(_context.Specializations);
        await _context.SaveChangesAsync();

        //// Dodaj specjalizacje
        var kardiologia = new Specialization { Name = "Kardiologia" };
        var kardiologiaDziecieca = new Specialization { Name = "Kardiologia Dziecięca" };
        var nefrologia = new Specialization { Name = "Nefrologia" };
        var interna = new Specialization { Name = "Interna" };

        _context.Specializations.AddRange(kardiologia, kardiologiaDziecieca, nefrologia, interna);
        await _context.SaveChangesAsync();

        // Dodaj lekarzy
        var karolSercowy = new Doctor { FirstName = "Karol", LastName = "Sercowy" };
        var piotrNerkowy = new Doctor { FirstName = "Piotr", LastName = "Nerkowy" };
        var mikolajWewnetrzny = new Doctor { FirstName = "Mikołaj", LastName = "Wewnętrzny" };

        _context.Doctors.AddRange(karolSercowy, piotrNerkowy, mikolajWewnetrzny);
        await _context.SaveChangesAsync();

        _context.Appointments.AddRange(
            new Appointment
            {
                DoctorId = karolSercowy.Id,
                StartTime = new DateTime(2025, 9, 16, 8, 30, 0),
                EndTime = new DateTime(2025, 9, 16, 9, 0, 0)
            },
            new Appointment
            {
                DoctorId = karolSercowy.Id,
                StartTime = new DateTime(2025, 9, 26, 12, 0, 0),
                EndTime = new DateTime(2025, 9, 26, 12, 30, 0)
            },
            new Appointment
            {
                DoctorId = piotrNerkowy.Id,
                StartTime = new DateTime(2025, 10, 1, 16, 5, 0),
                EndTime = new DateTime(2025, 10, 1, 16, 20, 0)
            },
            new Appointment
            {
                DoctorId = piotrNerkowy.Id,
                StartTime = new DateTime(2025, 10, 7, 12, 10, 0),
                EndTime = new DateTime(2025, 10, 7, 12, 20, 0)
            },
            new Appointment
            {
                DoctorId = mikolajWewnetrzny.Id,
                StartTime = new DateTime(2025, 9, 15, 11, 0, 0),
                EndTime = new DateTime(2025, 9, 15, 11, 20, 0)
            },
            new Appointment
            {
                DoctorId = mikolajWewnetrzny.Id,
                StartTime = new DateTime(2025, 9, 15, 16, 20, 0),
                EndTime = new DateTime(2025, 9, 15, 16, 40, 0)
            },
            new Appointment
            {
                DoctorId = mikolajWewnetrzny.Id,
                StartTime = new DateTime(2025, 9, 22, 16, 0, 0),
                EndTime = new DateTime(2025, 9, 22, 16, 10, 0)
            }
        );

        await _context.SaveChangesAsync();

        var kamilSercowySchedules = new[]
        {
            new Schedule
            {
                Doctor = karolSercowy,
                StartDate = new DateOnly(2025, 9, 16),
                EndDate = new DateOnly(2025, 9, 16),
                StartTime = new TimeOnly(8, 30, 0),
                EndTime = new TimeOnly(12, 30, 0),
                ScheduleSpecializations = [
                    new ScheduleSpecialization
                    {
                        Specialization = kardiologiaDziecieca
                    }
                ]
            },
            new Schedule
            {
                Doctor = karolSercowy,
                StartDate = new DateOnly(2025, 9, 17),
                EndDate = new DateOnly(2025, 9, 17),
                StartTime = new TimeOnly(9, 30, 0),
                EndTime = new TimeOnly(12, 30, 0),
                ScheduleSpecializations = [
                    new ScheduleSpecialization { Specialization = kardiologia },
                    new ScheduleSpecialization { Specialization = kardiologiaDziecieca }
                ]
            },
            new Schedule
            {
                DoctorId = karolSercowy.Id,
                StartDate = new DateOnly(2025, 9, 18),
                EndDate = new DateOnly(2025, 9, 18),
                StartTime = new TimeOnly(9, 0, 0),
                EndTime = new TimeOnly(12, 0, 0),
                ScheduleSpecializations = [
                    new ScheduleSpecialization { Specialization = kardiologiaDziecieca }
                ]
            }
            ,new Schedule
            {
                DoctorId = karolSercowy.Id,
                StartDate = new DateOnly(2025, 9, 22),
                DaysOfWeek = DayOfWeekFlags.Tuesday | DayOfWeekFlags.Friday,
                StartTime = new TimeOnly(10, 0, 0),
                EndTime = new TimeOnly(16, 0, 0),
                ScheduleSpecializations = [
                    new ScheduleSpecialization { Specialization = kardiologia },
                    new ScheduleSpecialization { Specialization = kardiologiaDziecieca }
                ]

            }
        };
        await _context.Schedules.AddRangeAsync(kamilSercowySchedules);
        await _context.SaveChangesAsync();

        var piotrNerkowySchedules = new[]
        {
            new Schedule
            {
                Doctor = piotrNerkowy,
                StartDate = new DateOnly(2025, 9, 1),
                EndDate = new DateOnly(2025, 10, 31),
                DaysOfWeek = DayOfWeekFlags.Wednesday,
                StartTime = new TimeOnly(16, 0, 0),
                EndTime = new TimeOnly(18, 0, 0),
                ScheduleSpecializations = [
                    new ScheduleSpecialization { Specialization = nefrologia },
                ]
            },
             new Schedule
            {
                Doctor = piotrNerkowy,
                StartDate = new DateOnly(2025, 9, 1),
                EndDate = new DateOnly(2025, 10, 31),
                DaysOfWeek = DayOfWeekFlags.Tuesday,
                StartTime = new TimeOnly(12, 0, 0),
                EndTime = new TimeOnly(15, 0, 0),
                ScheduleSpecializations = [
                    new ScheduleSpecialization { Specialization = nefrologia },
                ]
            },
              new Schedule
            {
                Doctor = piotrNerkowy,
                StartDate = new DateOnly(2025, 9, 1),
                DaysOfWeek = DayOfWeekFlags.Friday,
                StartTime = new TimeOnly(8, 0, 0),
                EndTime = new TimeOnly(11, 0, 0),
                ScheduleSpecializations = [
                    new ScheduleSpecialization { Specialization = nefrologia },
                ]
            }
        };

        await _context.Schedules.AddRangeAsync(piotrNerkowySchedules);
        await _context.SaveChangesAsync();

        var mikolajWewnetrznySchedules = new[]{
            new Schedule
            {
                Doctor = mikolajWewnetrzny,
                StartDate = new DateOnly(2025, 9, 4),
                EndDate = new DateOnly(2025, 9, 4),
                StartTime = new TimeOnly(16, 30, 0),
                EndTime = new TimeOnly(18, 30, 0),
                ScheduleSpecializations = [
                    new ScheduleSpecialization { Specialization = interna }
                ]
            },
            new Schedule
            {
                Doctor = mikolajWewnetrzny,
                StartDate = new DateOnly(2025, 9, 5),
                EndDate = new DateOnly(2025, 10, 31),
                DaysOfWeek = DayOfWeekFlags.Tuesday,
                StartTime = new TimeOnly(12, 0, 0),
                EndTime = new TimeOnly(15, 0, 0),
                ScheduleSpecializations = [
                    new ScheduleSpecialization { Specialization = nefrologia }
                ]
            },
            new Schedule
            {
                Doctor = mikolajWewnetrzny,
                StartDate = new DateOnly(2025, 1, 8),
                DaysOfWeek = DayOfWeekFlags.Monday,
                StartTime = new TimeOnly(8, 0, 0),
                EndTime = new TimeOnly(11, 0, 0),
                ScheduleSpecializations = [
                    new ScheduleSpecialization { Specialization = nefrologia }
                ]
            },
            new Schedule
            {
                Doctor = mikolajWewnetrzny,
                StartDate = new DateOnly(2025, 1, 8),
                DaysOfWeek = DayOfWeekFlags.Monday,
                StartTime = new TimeOnly(11, 0, 0),
                EndTime = new TimeOnly(14, 0, 0),
                ScheduleSpecializations = [
                    new ScheduleSpecialization { Specialization = interna },
                    new ScheduleSpecialization { Specialization = kardiologia }
                ]
            },
            new Schedule
            {
                Doctor = mikolajWewnetrzny,
                StartDate = new DateOnly(2025, 9, 8),
                DaysOfWeek = DayOfWeekFlags.Monday,
                StartTime = new TimeOnly(16, 0, 0),
                EndTime = new TimeOnly(18, 0, 0),
                ScheduleSpecializations = [
                    new ScheduleSpecialization { Specialization = interna },
                    new ScheduleSpecialization { Specialization = kardiologia },
                    new ScheduleSpecialization { Specialization = nefrologia }
                ]
            }
        };

        await _context.Schedules.AddRangeAsync(mikolajWewnetrznySchedules);
        await _context.SaveChangesAsync();



        return Ok(new
        {
            Message = "Database initialized successfully",
            DoctorsCount = await _context.Doctors.CountAsync(),
            SpecializationsCount = await _context.Specializations.CountAsync(),
            SchedulesCount = await _context.Schedules.CountAsync(),
            ScheduleSpecializationsCount = await _context.ScheduleSpecializations.CountAsync(),
            AppointmentsCount = await _context.Appointments.CountAsync()
        });
    }
}
