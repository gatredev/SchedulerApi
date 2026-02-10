using Scheduler.Api.Models;

namespace Scheduler.Api.Extensions;

public static class DayOfWeekFlagsExtensions
{
    public static bool HasDay(this DayOfWeekFlags flags, DayOfWeek day)
    {
        var flag = day switch
        {
            DayOfWeek.Monday => DayOfWeekFlags.Monday,
            DayOfWeek.Tuesday => DayOfWeekFlags.Tuesday,
            DayOfWeek.Wednesday => DayOfWeekFlags.Wednesday,
            DayOfWeek.Thursday => DayOfWeekFlags.Thursday,
            DayOfWeek.Friday => DayOfWeekFlags.Friday,
            DayOfWeek.Saturday => DayOfWeekFlags.Saturday,
            DayOfWeek.Sunday => DayOfWeekFlags.Sunday,
            _ => DayOfWeekFlags.None
        };

        return (flags & flag) == flag;
    }
}
