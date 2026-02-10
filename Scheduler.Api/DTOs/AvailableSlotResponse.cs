namespace Scheduler.Api.DTOs;

public class AvailableSlotResponse
{
    public string DoctorName { get; set; }
    public string SpecializationName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
