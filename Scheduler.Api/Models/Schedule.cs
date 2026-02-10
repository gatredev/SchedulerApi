namespace Scheduler.Api.Models;

public class Schedule
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public DayOfWeekFlags DaysOfWeek { get; set; } = DayOfWeekFlags.Everyday;

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public ICollection<ScheduleSpecialization> ScheduleSpecializations { get; set; } = new List<ScheduleSpecialization>();
}

[Flags]
public enum DayOfWeekFlags
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64,

    Everyday = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday
}
