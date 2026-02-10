using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Extensions;
using Scheduler.Api.Models;
using System.Collections;
using System.Runtime.InteropServices;

namespace Scheduler.Api.Services;

public class CalendarService
{
    private readonly ApplicationDbContext _context;

    public CalendarService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<object> Test(AvailableSlotsRequest request)
    {
        // TODO: Adjust date from year. -2 added for testing
        var effectiveDateFrom = new[] { request.DateFrom, DateOnly.FromDateTime(DateTime.Now.AddYears(-2)) }.Max()!.Value;
        var effectiveDateTo = new[] { request.DateTo, effectiveDateFrom.AddDays(31) }.Min()!.Value;

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

        var durationTicks = request.SlotDurationMinutes * TimeSpan.TicksPerSecond;

        scheduleCandidatesQuery = scheduleCandidatesQuery
            .Where(i => !(effectiveDateFrom > i.Schedule.EndDate))
            .Where(i => !(effectiveDateTo < i.Schedule.StartDate));

        var scheduleCandidates = await scheduleCandidatesQuery.ToListAsync();

        scheduleCandidates = scheduleCandidates
            .Where(i => (i.Schedule.EndTime - i.Schedule.StartTime) >= TimeSpan.FromMinutes(request.SlotDurationMinutes)).ToList();

        if (!scheduleCandidates.Any())
        {
            return new ();
        }

        var slotCandidates = scheduleCandidates.Select(i => new
        {
            i.Schedule.StartDate,
            i.Schedule.EndDate,
            i.Schedule.StartTime,
            i.Schedule.EndTime,
            i.Schedule.DaysOfWeek,
            DoctorName = $"{i.Schedule.Doctor.FirstName} {i.Schedule.Doctor.LastName}",
            SpecializationName = i.Specialization.Name
        });

        // build week
        var week = new Dictionary<DateOnly, List<KeyValuePair<string, KeyValuePair<TimeSpan, TimeSpan>>>>();

        var d = effectiveDateFrom;
        var toTake = request.MaxResults;
        while (d <= effectiveDateTo && toTake > 0)
        {
            var dailySlotCandidates = slotCandidates
                .Where(i => i.StartDate <= d)
                .Where(i => !i.EndDate.HasValue || d <= i.EndDate)
                .ToList();

            var slots = new List<KeyValuePair<string, KeyValuePair<TimeSpan, TimeSpan>>>();

            foreach(var dailySlot in dailySlotCandidates)
            {
                if (!dailySlot.DaysOfWeek.HasDay(d.DayOfWeek))
                {
                    continue;
                }

                var startTime = dailySlot.StartTime;
                var endTime = startTime + TimeSpan.FromMinutes(request.SlotDurationMinutes);
                var doctor = dailySlot.DoctorName;

                while (endTime <= dailySlot.EndTime)
                {
                    slots.Add(KeyValuePair.Create(doctor, KeyValuePair.Create(startTime, endTime)));

                    startTime = endTime;
                    endTime += TimeSpan.FromMinutes(request.SlotDurationMinutes);
                }
            }


            week[d] = slots.OrderBy(i => i.Value.Key).Take(toTake).ToList();
            toTake -= slots.Count;

            d = d.AddDays(1);
        }
        var s = new List<AvailableSlotResponse>();
        foreach(var day in week)
        {

            var ss = day.Value.Select(i => new AvailableSlotResponse
            {
                DoctorName = i.Key,
                StartTime = day.Key.ToDateTime(TimeOnly.FromTimeSpan(i.Value.Key)),
                EndTime = day.Key.ToDateTime(TimeOnly.FromTimeSpan(i.Value.Value)),
                SpecializationName = slotCandidates.First().SpecializationName,
            });

            s.AddRange(ss);
        }

        s = s.OrderBy(i => i.StartTime).ToList();

        return s;
    }

    //public async Task<List<AvailableSlotResponse>> FindAvailableSlots(AvailableSlotsRequest request)
    //{
    //    var effectiveDateFrom = new[] { request.DateFrom, DateOnly.FromDateTime(DateTime.Now) }.Max()!.Value;
    //    var effectiveDateTo = new[] { request.DateTo, effectiveDateFrom.AddDays(7) }.Min()!.Value;

    //    // Pobierz grafiki dla danej specjalizacji (i opcjonalnie lekarza)
    //    var scheduleCandidatesQuery = _context.ScheduleSpecializations
    //        .Include(s => s.Schedule)
    //            .ThenInclude(i => i.Doctor)
    //        .Include(i => i.Specialization)
    //        .Where(i => i.SpecializationId == request.SpecializationId);

    //    if (request.DoctorId.HasValue)
    //    {
    //        scheduleCandidatesQuery = scheduleCandidatesQuery.Where(s => s.Schedule.DoctorId == request.DoctorId.Value);
    //    }

    //    scheduleCandidatesQuery = scheduleCandidatesQuery
    //        .Where(i => !(effectiveDateFrom > i.Schedule.EndDate))
    //        .Where(i => !(effectiveDateTo < i.Schedule.StartDate))
    //        .Where(i => (i.Schedule.EndTime - i.Schedule.StartTime).TotalMinutes >= request.SlotDurationMinutes);

    //    var scheduleCandidates = await scheduleCandidatesQuery.ToListAsync();

    //    if (!scheduleCandidates.Any())
    //    {
    //        return new List<AvailableSlotResponse>();
    //    }

    //    var slotCandidates = scheduleCandidates.Select(i => new
    //    {
    //        i.Schedule.StartDate,
    //        i.Schedule.EndDate,
    //        i.Schedule.StartTime,
    //        i.Schedule.EndTime,
    //        i.Schedule.DaysOfWeek,
    //    });

    //    // build week

    //    var week = new Dictionary<DateOnly, string[]>();

    //    var d = effectiveDateFrom;
    //    while (d <= effectiveDateTo)
    //    {
    //        var dailySlotCandidates = slotCandidates
    //            .Where(i => i.StartDate <= d)
    //            .Where(i => d <= i.EndDate);

    //        week[d] = dailySlotCandidates.Select(i => $"{i.StartTime}-{i.EndTime}").ToArray();

    //        d.AddDays(1);
    //    }

        //foreach(var d in effectiveDateFrom)
        //{

        //}

        //foreach (var slot in slotCandidates) 
        //{
        //    var date = slot.


        //}

        //// Pobierz zajęte terminy dla lekarzy z grafików
        //var doctorIds = scheduleCandidates.Select(s => s.Schedule.DoctorId).Distinct().ToList();
        //var app = from appointment in _context.Appointments

        ////var appointments = await _context.Appointments
        ////    .Where(a => doctorIds.Contains(a.DoctorId))
        ////    .Where(a => a.EndTime >= effectiveDateFrom && a.StartTime <= effectiveDateTo)
        ////    .ToListAsync();

        //var availableSlots = new List<AvailableSlotResponse>();

        //foreach (var schedule in schedules)
        //{
        //    var scheduleDates = GetScheduleDates(schedule, dateFrom, dateTo);
        //    var doctorAppointments = appointments.Where(a => a.DoctorId == schedule.DoctorId).ToList();
        //    var specialization = schedule.ScheduleSpecializations
        //        .First(ss => ss.SpecializationId == request.SpecializationId)
        //        .Specialization;

        //    foreach (var date in scheduleDates)
        //    {
        //        var slots = GenerateSlots(
        //            date,
        //            schedule.StartTime,
        //            schedule.EndTime,
        //            request.SlotDurationMinutes,
        //            doctorAppointments
        //        );

        //        availableSlots.AddRange(slots.Select(slotStart => new AvailableSlotResponse
        //        {
        //            DoctorFirstName = schedule.Doctor.FirstName,
        //            DoctorLastName = schedule.Doctor.LastName,
        //            SpecializationName = specialization.Name,
        //            StartTime = slotStart
        //        }));

        //        if (availableSlots.Count >= request.MaxResults)
        //        {
        //            return availableSlots
        //                .OrderBy(s => s.StartTime)
        //                .Take(request.MaxResults)
        //                .ToList();
        //        }
        //    }
        //}

        //return availableSlots
        //    .OrderBy(s => s.StartTime)
        //    .Take(request.MaxResults)
        //    .ToList();
    //}

    //private List<DateTime> GetScheduleDates(Schedule schedule, DateTime dateFrom, DateTime dateTo)
    //{
    //    var dates = new List<DateTime>();

    //    if (!schedule.IsRecurring && schedule.SingleDate.HasValue)
    //    {
    //        // Grafik pojedynczy
    //        if (schedule.SingleDate.Value >= dateFrom.Date && schedule.SingleDate.Value <= dateTo.Date)
    //        {
    //            dates.Add(schedule.SingleDate.Value);
    //        }
    //    }
    //    else if (schedule.IsRecurring && schedule.RecurringStartDate.HasValue && schedule.DaysOfWeek.HasValue)
    //    {
    //        // Grafik cykliczny
    //        var startDate = schedule.RecurringStartDate.Value > dateFrom.Date
    //            ? schedule.RecurringStartDate.Value
    //            : dateFrom.Date;
    //        var endDate = schedule.RecurringEndDate.HasValue && schedule.RecurringEndDate.Value < dateTo.Date
    //            ? schedule.RecurringEndDate.Value
    //            : dateTo.Date;

    //        var currentDate = startDate;
    //        while (currentDate <= endDate)
    //        {
    //            var dayOfWeekFlag = GetDayOfWeekFlag(currentDate.DayOfWeek);
    //            if ((schedule.DaysOfWeek.Value & dayOfWeekFlag) != 0)
    //            {
    //                dates.Add(currentDate);
    //            }
    //            currentDate = currentDate.AddDays(1);
    //        }
    //    }

    //    return dates;
    //}

    private int GetDayOfWeekFlag(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => 1,
            DayOfWeek.Tuesday => 2,
            DayOfWeek.Wednesday => 4,
            DayOfWeek.Thursday => 8,
            DayOfWeek.Friday => 16,
            DayOfWeek.Saturday => 32,
            DayOfWeek.Sunday => 64,
            _ => 0
        };
    }

    private List<DateTime> GenerateSlots(
        DateTime date,
        TimeSpan startTime,
        TimeSpan endTime,
        int slotDurationMinutes,
        List<Appointment> appointments)
    {
        var slots = new List<DateTime>();
        var currentTime = date.Add(startTime);
        var scheduleEnd = date.Add(endTime);
        var now = DateTime.Now;

        while (currentTime.Add(TimeSpan.FromMinutes(slotDurationMinutes)) <= scheduleEnd)
        {
            var slotEnd = currentTime.Add(TimeSpan.FromMinutes(slotDurationMinutes));

            // Sprawdź czy slot jest w przyszłości
            if (currentTime <= now)
            {
                currentTime = currentTime.AddMinutes(1);
                continue;
            }

            // Sprawdź czy slot nie koliduje z istniejącymi rezerwacjami
            var hasConflict = appointments.Any(a =>
                (currentTime >= a.StartTime && currentTime < a.EndTime) ||
                (slotEnd > a.StartTime && slotEnd <= a.EndTime) ||
                (currentTime <= a.StartTime && slotEnd >= a.EndTime)
            );

            if (!hasConflict)
            {
                slots.Add(currentTime);
            }

            currentTime = currentTime.AddMinutes(1);
        }

        return slots;
    }
}
