namespace Scheduler.Api.Models;

public class Specialization
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public ICollection<ScheduleSpecialization> ScheduleSpecializations { get; set; } = new List<ScheduleSpecialization>();
}
