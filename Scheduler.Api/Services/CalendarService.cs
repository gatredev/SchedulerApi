using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Extensions;
using Scheduler.Api.Models;

namespace Scheduler.Api.Services;

public class CalendarService
{
    private readonly ApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;

    public CalendarService(ApplicationDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<List<AvailableSlotResponse>> BuildCalendarAsync(AvailableSlotsRequest request)
    {
        var effectiveDateFrom = new[] { request.DateFrom, DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime) }.Max()!.Value;
        var effectiveDateTo = new[] { request.DateTo, effectiveDateFrom.AddMonths(3) }.Min()!.Value;

        // Pobierz grafiki dla danej specjalizacji (i opcjonalnie lekarza)
        var scheduleCandidatesQuery = _context.ScheduleSpecializations.AsNoTracking()
            .Include(s => s.Schedule)
                .ThenInclude(i => i.Doctor)
            .Include(i => i.Specialization)
            .Where(i => i.SpecializationId == request.SpecializationId);

        if (request.DoctorId.HasValue)
        {
            scheduleCandidatesQuery = scheduleCandidatesQuery.Where(s => s.Schedule.DoctorId == request.DoctorId.Value);
        }

        scheduleCandidatesQuery = scheduleCandidatesQuery
            .Where(i => !(effectiveDateFrom > i.Schedule.EndDate))
            .Where(i => !(effectiveDateTo < i.Schedule.StartDate));

        var scheduleConfigurationCandidates = await scheduleCandidatesQuery.ToListAsync();

        if (!scheduleConfigurationCandidates.Any())
        {
            return new();
        }

        // check for general slot availability
        // This could be moved to sql execution if used sqlServer with EF.Functions
        // or postgres using tstzrange type
        scheduleConfigurationCandidates = scheduleConfigurationCandidates
            .Where(i => (i.Schedule.EndTime - i.Schedule.StartTime) >= TimeSpan.FromMinutes(request.SlotDurationMinutes)).ToList();

        if (!scheduleConfigurationCandidates.Any())
        {
            return new();
        }

        var scheduleConfigurations = scheduleConfigurationCandidates.Select(i => new ScheduleConfiguration
        {
            StartDate = i.Schedule.StartDate,
            EndDate = i.Schedule.EndDate,
            StartTime = i.Schedule.StartTime,
            EndTime = i.Schedule.EndTime,
            DaysOfWeek = i.Schedule.DaysOfWeek,
            DoctorName = $"{i.Schedule.Doctor.FirstName} {i.Schedule.Doctor.LastName}",
            DoctorId = i.Schedule.DoctorId,
            SpecializationName = i.Specialization.Name
        }).ToList();

        var cal = await BuildCalendarAsync(scheduleConfigurations, effectiveDateFrom, effectiveDateTo, request);
        return cal;
    }

    private async Task<List<AvailableSlotResponse>> BuildCalendarAsync(List<ScheduleConfiguration> configurations, DateOnly dateFrom, DateOnly dateTo, AvailableSlotsRequest request)
    {
        var appointments = await GetAppointmentsAsync(dateFrom, dateTo, configurations.Select(i => i.DoctorId).Distinct().ToArray());

        var calendar = new List<AvailableSlotResponse>();
        var dateCursor = dateFrom;
        while (dateCursor <= dateTo && calendar.Count < request.MaxResults)
        {
            var dailyAppointments = appointments.Where(i => i.StartTime.Date == dateCursor.ToDateTime(TimeOnly.MinValue)).ToList();

            var dailyConfigurationCandidates = configurations
                .Where(i => i.StartDate <= dateCursor)
                .Where(i => !i.EndDate.HasValue || dateCursor <= i.EndDate)
                .ToList();

            foreach (var dailyConfigurationCandidate in dailyConfigurationCandidates)
            {
                if (dailyConfigurationCandidate.StartDate != dailyConfigurationCandidate.EndDate
                    && !dailyConfigurationCandidate.DaysOfWeek.HasDay(dateCursor.DayOfWeek))
                    continue;

                var doctorDailyAppointments = dailyAppointments.Where(i => i.DoctorId == dailyConfigurationCandidate.DoctorId).ToList();
                var slots = await BuildSlotsAsync(dailyConfigurationCandidate, doctorDailyAppointments, request.SlotDurationMinutes, dateCursor);

                calendar.AddRange(
                    slots.Select(i => new AvailableSlotResponse
                    {
                        SpecializationName = dailyConfigurationCandidate.SpecializationName,
                        DoctorName = dailyConfigurationCandidate.DoctorName,
                        StartTime = dateCursor.ToDateTime(i.startTime),
                        EndTime = dateCursor.ToDateTime(i.endTime),
                    }));
            }

            dateCursor = dateCursor.AddDays(1);
        }

        return calendar.OrderBy(i => i.StartTime).Take(request.MaxResults).ToList();
    }

    private async Task<List<Appointment>> GetAppointmentsAsync(DateOnly from, DateOnly to, int[] doctorIds)
    {
        var fromDateTime = from.ToDateTime(TimeOnly.MinValue);
        var toDateTime = to.ToDateTime(TimeOnly.MaxValue);

        var appointments = await _context.Appointments.AsNoTracking()
            .Where(i => doctorIds.Contains(i.DoctorId))
            .Where(i => !(fromDateTime > i.EndTime))
            .Where(i => !(toDateTime < i.StartTime))
            .ToListAsync();

        return appointments;
    }

    private async Task<List<(TimeOnly startTime, TimeOnly endTime)>> BuildSlotsAsync(ScheduleConfiguration slotCandidate, List<Appointment> appointments, int slotDurationMinutes, DateOnly dateCursor)
    {
        var overlappingAppointments = appointments
            .Where(i => !(i.StartTime.TimeOfDay > slotCandidate.EndTime.ToTimeSpan()))
            .Where(i => !(i.EndTime.TimeOfDay < slotCandidate.StartTime.ToTimeSpan()))
            .ToList();

        var freeSlots = new List<(TimeOnly Start, TimeOnly End)>();

        var slotStartTime = slotCandidate.StartTime;

        // prevent slot starting time before now 
        if (dateCursor.ToDateTime(slotStartTime) < _timeProvider.GetLocalNow().DateTime)
        {
            slotStartTime = TimeOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        }

        foreach (var appointment in overlappingAppointments.OrderBy(i => i.StartTime))
        {
            if (slotStartTime.AddMinutes(slotDurationMinutes).ToTimeSpan() > appointment.StartTime.TimeOfDay)
            {
                slotStartTime = TimeOnly.FromTimeSpan(appointment.EndTime.TimeOfDay);
                continue;
            }

            var slotEndTime = TimeOnly.FromTimeSpan(appointment.StartTime.TimeOfDay);
            freeSlots.Add((slotStartTime, slotEndTime));
            slotStartTime = TimeOnly.FromTimeSpan(appointment.EndTime.TimeOfDay);
        }

        if (slotStartTime.AddMinutes(slotDurationMinutes).ToTimeSpan() <= slotCandidate.EndTime.ToTimeSpan())
        {
            freeSlots.Add((slotStartTime, slotCandidate.EndTime));
        }

        var effectiveFreeSlots = freeSlots.SelectMany(i => SplitSlotsBySlotTime(i, slotDurationMinutes)).ToList();

        return effectiveFreeSlots;
    }


    private List<(TimeOnly startTime, TimeOnly endTime)> SplitSlotsBySlotTime((TimeOnly startTime, TimeOnly endTime) slot, int slotDurationMinutes)
    {
        var freeSlots = new List<(TimeOnly Start, TimeOnly End)>();

        var slotStartTime = slot.startTime;
        var slotEndTime = slotStartTime.AddMinutes(slotDurationMinutes);
        while (slotEndTime <= slot.endTime)
        {
            freeSlots.Add((slotStartTime, slotEndTime));
            slotStartTime = slotEndTime;
            slotEndTime = slotEndTime.AddMinutes(slotDurationMinutes);
        }

        return freeSlots;
    }
}
