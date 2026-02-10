using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Models;
using Scheduler.Api.Services;
using Xunit;

namespace Scheduler.Api.Tests.Services;

public class CalendarServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CalendarService _service;

    public CalendarServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(new DateTime(2023, 1, 1, 12, 0, 0));
        _service = new CalendarService(_context, fakeTimeProvider);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region BuildCalendarAsync Tests

    [Fact]
    public async Task BuildCalendarAsync_AllSchedulesExpired_ReturnsEmptyList()
    {
        // Arrange
        var specialization = new Specialization { Id = 1, Name = "Kardiologia" };
        var doctor = new Doctor { Id = 1, FirstName = "Jan", LastName = "Kowalski" };

        var schedule = new Schedule
        {
            Id = 1,
            DoctorId = doctor.Id,
            Doctor = doctor,
            StartDate = new DateOnly(2022, 9, 15),
            EndDate = new DateOnly(2022, 9, 15),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            ScheduleSpecializations = new List<ScheduleSpecialization>
            {
                new ScheduleSpecialization { SpecializationId = specialization.Id, Specialization = specialization }
            }
        };

        _context.Specializations.Add(specialization);
        _context.Doctors.Add(doctor);
        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        var request = new AvailableSlotsRequest
        {
            SpecializationId = specialization.Id,
            DateFrom = new DateOnly(2022, 1, 1),
            DateTo = new DateOnly(2025, 9, 30),
            SlotDurationMinutes = 30,
            MaxResults = 10
        };

        // Act
        var result = await _service.BuildCalendarAsync(request);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildCalendarAsync_ScheduleForTodayOverlapingHours_ReturnsAdjustedStartTime()
    {
        // Arrange
        var specialization = new Specialization { Id = 1, Name = "Kardiologia" };
        var doctor = new Doctor { Id = 1, FirstName = "Jan", LastName = "Kowalski" };

        var schedule = new Schedule
        {
            Id = 1,
            DoctorId = doctor.Id,
            Doctor = doctor,
            StartDate = new DateOnly(2022, 9, 15),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(20, 0),
            ScheduleSpecializations = new List<ScheduleSpecialization>
            {
                new ScheduleSpecialization { SpecializationId = specialization.Id, Specialization = specialization }
            }
        };

        _context.Specializations.Add(specialization);
        _context.Doctors.Add(doctor);
        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        var request = new AvailableSlotsRequest
        {
            SpecializationId = specialization.Id,
            DateFrom = new DateOnly(2022, 9, 1),
            DateTo = new DateOnly(2025, 9, 30),
            SlotDurationMinutes = 30,
            MaxResults = 10
        };

        // Act
        var result = await _service.BuildCalendarAsync(request);

        // Assert
        result[0].StartTime.Should().Be(new DateTime(2023, 1, 1, 12, 0, 0));
    }

    [Fact]
    public async Task BuildCalendarAsync_NoSchedulesFound_ReturnsEmptyList()
    {
        // Arrange
        var request = new AvailableSlotsRequest
        {
            SpecializationId = 999,
            DateFrom = new DateOnly(2025, 9, 1),
            DateTo = new DateOnly(2025, 9, 30),
            SlotDurationMinutes = 30,
            MaxResults = 10
        };

        // Act
        var result = await _service.BuildCalendarAsync(request);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildCalendarAsync_ScheduleTooShortForSlot_ReturnsEmptyList()
    {
        // Arrange
        var specialization = new Specialization { Id = 1, Name = "Kardiologia" };
        var doctor = new Doctor { Id = 1, FirstName = "Jan", LastName = "Kowalski" };

        var schedule = new Schedule
        {
            Id = 1,
            DoctorId = doctor.Id,
            Doctor = doctor,
            StartDate = new DateOnly(2025, 9, 1),
            EndDate = new DateOnly(2025, 9, 1),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 15), // Only 15 minutes
            ScheduleSpecializations = new List<ScheduleSpecialization>
            {
                new ScheduleSpecialization { SpecializationId = specialization.Id, Specialization = specialization }
            }
        };

        _context.Specializations.Add(specialization);
        _context.Doctors.Add(doctor);
        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        var request = new AvailableSlotsRequest
        {
            SpecializationId = specialization.Id,
            DateFrom = new DateOnly(2025, 9, 1),
            DateTo = new DateOnly(2025, 9, 30),
            SlotDurationMinutes = 30, // Longer than schedule duration
            MaxResults = 10
        };

        // Act
        var result = await _service.BuildCalendarAsync(request);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildCalendarAsync_SingleDayScheduleNoAppointments_ReturnsOneSlot()
    {
        // Arrange
        var specialization = new Specialization { Id = 1, Name = "Kardiologia" };
        var doctor = new Doctor { Id = 1, FirstName = "Jan", LastName = "Kowalski" };

        var schedule = new Schedule
        {
            Id = 1,
            DoctorId = doctor.Id,
            Doctor = doctor,
            StartDate = new DateOnly(2025, 9, 15),
            EndDate = new DateOnly(2025, 9, 15),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            ScheduleSpecializations = new List<ScheduleSpecialization>
            {
                new ScheduleSpecialization { SpecializationId = specialization.Id, Specialization = specialization }
            }
        };

        _context.Specializations.Add(specialization);
        _context.Doctors.Add(doctor);
        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        var request = new AvailableSlotsRequest
        {
            SpecializationId = specialization.Id,
            DateFrom = new DateOnly(2025, 9, 1),
            DateTo = new DateOnly(2025, 9, 30),
            SlotDurationMinutes = 30,
            MaxResults = 10
        };

        // Act
        var result = await _service.BuildCalendarAsync(request);

        // Assert
        result.Should().HaveCount(1);
        result[0].SpecializationName.Should().Be("Kardiologia");
        result[0].DoctorName.Should().Be("Jan Kowalski");
        result[0].StartTime.Should().Be(new DateTime(2025, 9, 15, 10, 0, 0));
        result[0].EndTime.Should().Be(new DateTime(2025, 9, 15, 12, 0, 0));
    }

    [Fact]
    public async Task BuildCalendarAsync_WithAppointmentInMiddle_ReturnsTwoSlots()
    {
        // Arrange
        var specialization = new Specialization { Id = 1, Name = "Kardiologia" };
        var doctor = new Doctor { Id = 1, FirstName = "Jan", LastName = "Kowalski" };

        var schedule = new Schedule
        {
            Id = 1,
            DoctorId = doctor.Id,
            Doctor = doctor,
            StartDate = new DateOnly(2025, 9, 15),
            EndDate = new DateOnly(2025, 9, 15),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(14, 0),
            ScheduleSpecializations = new List<ScheduleSpecialization>
            {
                new ScheduleSpecialization { SpecializationId = specialization.Id, Specialization = specialization }
            }
        };

        var appointment = new Appointment
        {
            Id = 1,
            DoctorId = doctor.Id,
            StartTime = new DateTime(2025, 9, 15, 11, 0, 0),
            EndTime = new DateTime(2025, 9, 15, 11, 30, 0)
        };

        _context.Specializations.Add(specialization);
        _context.Doctors.Add(doctor);
        _context.Schedules.Add(schedule);
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        var request = new AvailableSlotsRequest
        {
            SpecializationId = specialization.Id,
            DateFrom = new DateOnly(2025, 9, 1),
            DateTo = new DateOnly(2025, 9, 30),
            SlotDurationMinutes = 30,
            MaxResults = 10
        };

        // Act
        var result = await _service.BuildCalendarAsync(request);

        // Assert
        result.Should().HaveCount(2);

        // First slot: 10:00 - 11:00
        result[0].StartTime.Should().Be(new DateTime(2025, 9, 15, 10, 0, 0));
        result[0].EndTime.Should().Be(new DateTime(2025, 9, 15, 11, 0, 0));

        // Second slot: 11:30 - 14:00
        result[1].StartTime.Should().Be(new DateTime(2025, 9, 15, 11, 30, 0));
        result[1].EndTime.Should().Be(new DateTime(2025, 9, 15, 14, 0, 0));
    }

    [Fact]
    public async Task BuildCalendarAsync_MultipleAppointments_ReturnsCorrectSlots()
    {
        // Arrange
        var specialization = new Specialization { Id = 1, Name = "Kardiologia" };
        var doctor = new Doctor { Id = 1, FirstName = "Jan", LastName = "Kowalski" };

        var schedule = new Schedule
        {
            Id = 1,
            DoctorId = doctor.Id,
            Doctor = doctor,
            StartDate = new DateOnly(2025, 9, 15),
            EndDate = new DateOnly(2025, 9, 15),
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(16, 0),
            ScheduleSpecializations = new List<ScheduleSpecialization>
            {
                new ScheduleSpecialization { SpecializationId = specialization.Id, Specialization = specialization }
            }
        };

        var appointments = new List<Appointment>
        {
            new Appointment { DoctorId = doctor.Id, StartTime = new DateTime(2025, 9, 15, 9, 0, 0), EndTime = new DateTime(2025, 9, 15, 9, 30, 0) },
            new Appointment { DoctorId = doctor.Id, StartTime = new DateTime(2025, 9, 15, 11, 0, 0), EndTime = new DateTime(2025, 9, 15, 11, 30, 0) },
            new Appointment { DoctorId = doctor.Id, StartTime = new DateTime(2025, 9, 15, 14, 0, 0), EndTime = new DateTime(2025, 9, 15, 14, 30, 0) }
        };

        _context.Specializations.Add(specialization);
        _context.Doctors.Add(doctor);
        _context.Schedules.Add(schedule);
        _context.Appointments.AddRange(appointments);
        await _context.SaveChangesAsync();

        var request = new AvailableSlotsRequest
        {
            SpecializationId = specialization.Id,
            DateFrom = new DateOnly(2025, 9, 1),
            DateTo = new DateOnly(2025, 9, 30),
            SlotDurationMinutes = 30,
            MaxResults = 10
        };

        // Act
        var result = await _service.BuildCalendarAsync(request);

        // Assert
        result.Should().HaveCount(4);
        result[0].StartTime.Should().Be(new DateTime(2025, 9, 15, 8, 0, 0));
        result[1].StartTime.Should().Be(new DateTime(2025, 9, 15, 9, 30, 0));
        result[2].StartTime.Should().Be(new DateTime(2025, 9, 15, 11, 30, 0));
        result[3].StartTime.Should().Be(new DateTime(2025, 9, 15, 14, 30, 0));
    }

    [Fact]
    public async Task BuildCalendarAsync_RecurringSchedule_ReturnsMultipleDays()
    {
        // Arrange
        var specialization = new Specialization { Id = 1, Name = "Kardiologia" };
        var doctor = new Doctor { Id = 1, FirstName = "Jan", LastName = "Kowalski" };

        // Monday and Wednesday schedule
        var schedule = new Schedule
        {
            Id = 1,
            DoctorId = doctor.Id,
            Doctor = doctor,
            StartDate = new DateOnly(2025, 9, 1), // September 1, 2025 is Monday
            EndDate = new DateOnly(2025, 9, 30),
            DaysOfWeek = DayOfWeekFlags.Monday | DayOfWeekFlags.Wednesday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            ScheduleSpecializations = new List<ScheduleSpecialization>
            {
                new ScheduleSpecialization { SpecializationId = specialization.Id, Specialization = specialization }
            }
        };

        _context.Specializations.Add(specialization);
        _context.Doctors.Add(doctor);
        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        var request = new AvailableSlotsRequest
        {
            SpecializationId = specialization.Id,
            DateFrom = new DateOnly(2025, 9, 1),
            DateTo = new DateOnly(2025, 9, 10),
            SlotDurationMinutes = 30,
            MaxResults = 10
        };

        // Act
        var result = await _service.BuildCalendarAsync(request);

        // Assert - Should have slots for: Sep 1 (Mon), Sep 3 (Wed), Sep 8 (Mon), Sep 10 (Wed)
        result.Should().HaveCount(4);
        result.Should().Contain(s => s.StartTime.Date == new DateTime(2025, 9, 1).Date);
        result.Should().Contain(s => s.StartTime.Date == new DateTime(2025, 9, 3).Date);
        result.Should().Contain(s => s.StartTime.Date == new DateTime(2025, 9, 8).Date);
        result.Should().Contain(s => s.StartTime.Date == new DateTime(2025, 9, 10).Date);
    }

    [Fact]
    public async Task BuildCalendarAsync_FilterByDoctorId_ReturnsOnlyThatDoctor()
    {
        // Arrange
        var specialization = new Specialization { Id = 1, Name = "Kardiologia" };
        var doctor1 = new Doctor { Id = 1, FirstName = "Jan", LastName = "Kowalski" };
        var doctor2 = new Doctor { Id = 2, FirstName = "Anna", LastName = "Nowak" };

        var schedule1 = new Schedule
        {
            Id = 1,
            DoctorId = doctor1.Id,
            Doctor = doctor1,
            StartDate = new DateOnly(2025, 9, 15),
            EndDate = new DateOnly(2025, 9, 15),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            ScheduleSpecializations = new List<ScheduleSpecialization>
            {
                new ScheduleSpecialization { SpecializationId = specialization.Id, Specialization = specialization }
            }
        };

        var schedule2 = new Schedule
        {
            Id = 2,
            DoctorId = doctor2.Id,
            Doctor = doctor2,
            StartDate = new DateOnly(2025, 9, 15),
            EndDate = new DateOnly(2025, 9, 15),
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(16, 0),
            ScheduleSpecializations = new List<ScheduleSpecialization>
            {
                new ScheduleSpecialization { SpecializationId = specialization.Id, Specialization = specialization }
            }
        };

        _context.Specializations.Add(specialization);
        _context.Doctors.AddRange(doctor1, doctor2);
        _context.Schedules.AddRange(schedule1, schedule2);
        await _context.SaveChangesAsync();

        var request = new AvailableSlotsRequest
        {
            SpecializationId = specialization.Id,
            DoctorId = doctor1.Id, // Filter by doctor 1
            DateFrom = new DateOnly(2025, 9, 1),
            DateTo = new DateOnly(2025, 9, 30),
            SlotDurationMinutes = 30,
            MaxResults = 10
        };

        // Act
        var result = await _service.BuildCalendarAsync(request);

        // Assert
        result.Should().HaveCount(1);
        result[0].DoctorName.Should().Be("Jan Kowalski");
        result.Should().NotContain(s => s.DoctorName == "Anna Nowak");
    }

    [Fact]
    public async Task BuildCalendarAsync_MaxResults_ReturnsLimitedSlots()
    {
        // Arrange
        var specialization = new Specialization { Id = 1, Name = "Kardiologia" };
        var doctor = new Doctor { Id = 1, FirstName = "Jan", LastName = "Kowalski" };

        var schedule = new Schedule
        {
            Id = 1,
            DoctorId = doctor.Id,
            Doctor = doctor,
            StartDate = new DateOnly(2025, 9, 1),
            EndDate = new DateOnly(2025, 9, 30),
            DaysOfWeek = DayOfWeekFlags.Monday | DayOfWeekFlags.Tuesday | DayOfWeekFlags.Wednesday |
                         DayOfWeekFlags.Thursday | DayOfWeekFlags.Friday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            ScheduleSpecializations = new List<ScheduleSpecialization>
            {
                new ScheduleSpecialization { SpecializationId = specialization.Id, Specialization = specialization }
            }
        };

        _context.Specializations.Add(specialization);
        _context.Doctors.Add(doctor);
        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        var request = new AvailableSlotsRequest
        {
            SpecializationId = specialization.Id,
            DateFrom = new DateOnly(2025, 9, 1),
            DateTo = new DateOnly(2025, 9, 30),
            SlotDurationMinutes = 30,
            MaxResults = 3 // Limit to 3 results
        };

        // Act
        var result = await _service.BuildCalendarAsync(request);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task BuildCalendarAsync_AppointmentAtStartOfSchedule_ReturnsSlotAfter()
    {
        // Arrange
        var specialization = new Specialization { Id = 1, Name = "Kardiologia" };
        var doctor = new Doctor { Id = 1, FirstName = "Jan", LastName = "Kowalski" };

        var schedule = new Schedule
        {
            Id = 1,
            DoctorId = doctor.Id,
            Doctor = doctor,
            StartDate = new DateOnly(2025, 9, 15),
            EndDate = new DateOnly(2025, 9, 15),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(14, 0),
            ScheduleSpecializations = new List<ScheduleSpecialization>
            {
                new ScheduleSpecialization { SpecializationId = specialization.Id, Specialization = specialization }
            }
        };

        var appointment = new Appointment
        {
            Id = 1,
            DoctorId = doctor.Id,
            StartTime = new DateTime(2025, 9, 15, 10, 0, 0),
            EndTime = new DateTime(2025, 9, 15, 10, 30, 0)
        };

        _context.Specializations.Add(specialization);
        _context.Doctors.Add(doctor);
        _context.Schedules.Add(schedule);
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        var request = new AvailableSlotsRequest
        {
            SpecializationId = specialization.Id,
            DateFrom = new DateOnly(2025, 9, 1),
            DateTo = new DateOnly(2025, 9, 30),
            SlotDurationMinutes = 30,
            MaxResults = 10
        };

        // Act
        var result = await _service.BuildCalendarAsync(request);

        // Assert
        result.Should().HaveCount(1);
        result[0].StartTime.Should().Be(new DateTime(2025, 9, 15, 10, 30, 0));
        result[0].EndTime.Should().Be(new DateTime(2025, 9, 15, 14, 0, 0));
    }

    [Fact]
    public async Task BuildCalendarAsync_AppointmentAtEndOfSchedule_ReturnsSlotBefore()
    {
        // Arrange
        var specialization = new Specialization { Id = 1, Name = "Kardiologia" };
        var doctor = new Doctor { Id = 1, FirstName = "Jan", LastName = "Kowalski" };

        var schedule = new Schedule
        {
            Id = 1,
            DoctorId = doctor.Id,
            Doctor = doctor,
            StartDate = new DateOnly(2025, 9, 15),
            EndDate = new DateOnly(2025, 9, 15),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(14, 0),
            ScheduleSpecializations = new List<ScheduleSpecialization>
            {
                new ScheduleSpecialization { SpecializationId = specialization.Id, Specialization = specialization }
            }
        };

        var appointment = new Appointment
        {
            Id = 1,
            DoctorId = doctor.Id,
            StartTime = new DateTime(2025, 9, 15, 13, 30, 0),
            EndTime = new DateTime(2025, 9, 15, 14, 0, 0)
        };

        _context.Specializations.Add(specialization);
        _context.Doctors.Add(doctor);
        _context.Schedules.Add(schedule);
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        var request = new AvailableSlotsRequest
        {
            SpecializationId = specialization.Id,
            DateFrom = new DateOnly(2025, 9, 1),
            DateTo = new DateOnly(2025, 9, 30),
            SlotDurationMinutes = 30,
            MaxResults = 10
        };

        // Act
        var result = await _service.BuildCalendarAsync(request);

        // Assert
        result.Should().HaveCount(1);
        result[0].StartTime.Should().Be(new DateTime(2025, 9, 15, 10, 0, 0));
        result[0].EndTime.Should().Be(new DateTime(2025, 9, 15, 13, 30, 0));
    }

    [Fact]
    public async Task BuildCalendarAsync_EntireScheduleBooked_ReturnsNoSlots()
    {
        // Arrange
        var specialization = new Specialization { Id = 1, Name = "Kardiologia" };
        var doctor = new Doctor { Id = 1, FirstName = "Jan", LastName = "Kowalski" };

        var schedule = new Schedule
        {
            Id = 1,
            DoctorId = doctor.Id,
            Doctor = doctor,
            StartDate = new DateOnly(2025, 9, 15),
            EndDate = new DateOnly(2025, 9, 15),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            ScheduleSpecializations = new List<ScheduleSpecialization>
            {
                new ScheduleSpecialization { SpecializationId = specialization.Id, Specialization = specialization }
            }
        };

        var appointment = new Appointment
        {
            Id = 1,
            DoctorId = doctor.Id,
            StartTime = new DateTime(2025, 9, 15, 10, 0, 0),
            EndTime = new DateTime(2025, 9, 15, 12, 0, 0)
        };

        _context.Specializations.Add(specialization);
        _context.Doctors.Add(doctor);
        _context.Schedules.Add(schedule);
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        var request = new AvailableSlotsRequest
        {
            SpecializationId = specialization.Id,
            DateFrom = new DateOnly(2025, 9, 1),
            DateTo = new DateOnly(2025, 9, 30),
            SlotDurationMinutes = 30,
            MaxResults = 10
        };

        // Act
        var result = await _service.BuildCalendarAsync(request);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildCalendarAsync_NoRemainingTimeAfterAppointment_SkipsSlot()
    {
        // Arrange
        var specialization = new Specialization { Id = 1, Name = "Kardiologia" };
        var doctor = new Doctor { Id = 1, FirstName = "Jan", LastName = "Kowalski" };

        var schedule = new Schedule
        {
            Id = 1,
            DoctorId = doctor.Id,
            Doctor = doctor,
            StartDate = new DateOnly(2025, 9, 15),
            EndDate = new DateOnly(2025, 9, 15),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            ScheduleSpecializations = new List<ScheduleSpecialization>
            {
                new ScheduleSpecialization { SpecializationId = specialization.Id, Specialization = specialization }
            }
        };

        // Appointment ends only 20 minutes before schedule end, but slot needs 30 minutes
        var appointment = new Appointment
        {
            Id = 1,
            DoctorId = doctor.Id,
            StartTime = new DateTime(2025, 9, 15, 10, 0, 0),
            EndTime = new DateTime(2025, 9, 15, 11, 40, 0)
        };

        _context.Specializations.Add(specialization);
        _context.Doctors.Add(doctor);
        _context.Schedules.Add(schedule);
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        var request = new AvailableSlotsRequest
        {
            SpecializationId = specialization.Id,
            DateFrom = new DateOnly(2025, 9, 1),
            DateTo = new DateOnly(2025, 9, 30),
            SlotDurationMinutes = 30,
            MaxResults = 10
        };

        // Act
        var result = await _service.BuildCalendarAsync(request);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildCalendarAsync_ScheduleOutsideDateRange_ReturnsNoSlots()
    {
        // Arrange
        var specialization = new Specialization { Id = 1, Name = "Kardiologia" };
        var doctor = new Doctor { Id = 1, FirstName = "Jan", LastName = "Kowalski" };

        var schedule = new Schedule
        {
            Id = 1,
            DoctorId = doctor.Id,
            Doctor = doctor,
            StartDate = new DateOnly(2025, 10, 15), // Outside requested range
            EndDate = new DateOnly(2025, 10, 15),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            ScheduleSpecializations = new List<ScheduleSpecialization>
            {
                new ScheduleSpecialization { SpecializationId = specialization.Id, Specialization = specialization }
            }
        };

        _context.Specializations.Add(specialization);
        _context.Doctors.Add(doctor);
        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        var request = new AvailableSlotsRequest
        {
            SpecializationId = specialization.Id,
            DateFrom = new DateOnly(2025, 9, 1),
            DateTo = new DateOnly(2025, 9, 30),
            SlotDurationMinutes = 30,
            MaxResults = 10
        };

        // Act
        var result = await _service.BuildCalendarAsync(request);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildCalendarAsync_MultipleDoctorsSameSpecialization_ReturnsAllSlots()
    {
        // Arrange
        var specialization = new Specialization { Id = 1, Name = "Kardiologia" };
        var doctor1 = new Doctor { Id = 1, FirstName = "Jan", LastName = "Kowalski" };
        var doctor2 = new Doctor { Id = 2, FirstName = "Anna", LastName = "Nowak" };

        var schedule1 = new Schedule
        {
            Id = 1,
            DoctorId = doctor1.Id,
            Doctor = doctor1,
            StartDate = new DateOnly(2025, 9, 15),
            EndDate = new DateOnly(2025, 9, 15),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            ScheduleSpecializations = new List<ScheduleSpecialization>
            {
                new ScheduleSpecialization { SpecializationId = specialization.Id, Specialization = specialization }
            }
        };

        var schedule2 = new Schedule
        {
            Id = 2,
            DoctorId = doctor2.Id,
            Doctor = doctor2,
            StartDate = new DateOnly(2025, 9, 15),
            EndDate = new DateOnly(2025, 9, 15),
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(16, 0),
            ScheduleSpecializations = new List<ScheduleSpecialization>
            {
                new ScheduleSpecialization { SpecializationId = specialization.Id, Specialization = specialization }
            }
        };

        _context.Specializations.Add(specialization);
        _context.Doctors.AddRange(doctor1, doctor2);
        _context.Schedules.AddRange(schedule1, schedule2);
        await _context.SaveChangesAsync();

        var request = new AvailableSlotsRequest
        {
            SpecializationId = specialization.Id,
            DateFrom = new DateOnly(2025, 9, 1),
            DateTo = new DateOnly(2025, 9, 30),
            SlotDurationMinutes = 30,
            MaxResults = 10
        };

        // Act
        var result = await _service.BuildCalendarAsync(request);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.DoctorName == "Jan Kowalski");
        result.Should().Contain(s => s.DoctorName == "Anna Nowak");
    }

    #endregion
}