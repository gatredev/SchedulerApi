namespace Scheduler.Api.Models;

public class Doctor
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public ICollection<Specialization> Specializations { get; set; } = new List<Specialization>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
