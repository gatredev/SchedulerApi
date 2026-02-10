using Scheduler.Api.Models;

namespace Scheduler.Api.DTOs;

public class ScheduleConfiguration
{
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TimeOnly StartTime { get;set; }
    public TimeOnly EndTime { get; set; }
    public DayOfWeekFlags DaysOfWeek { get; set; }
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = null!;
    public string SpecializationName { get; set; } = null!;
}
