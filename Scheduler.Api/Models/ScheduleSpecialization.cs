namespace Scheduler.Api.Models;

public class ScheduleSpecialization
{
    public int ScheduleId { get; set; }
    public int SpecializationId { get; set; }

    public Schedule Schedule { get; set; } = null!;
    public Specialization Specialization { get; set; } = null!;
}
