using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Models;

namespace Scheduler.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Specialization> Specializations { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<ScheduleSpecialization> ScheduleSpecializations { get; set; }
    public DbSet<Appointment> Appointments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ScheduleSpecialization>()
            .HasKey(i => new { i.ScheduleId, i.SpecializationId });


        modelBuilder.Entity<Appointment>().HasIndex(a => new { a.DoctorId, a.StartTime, a.EndTime });
        modelBuilder.Entity<ScheduleSpecialization>().HasIndex(x => x.SpecializationId);
        modelBuilder.Entity<Schedule>().HasIndex(s => new { s.DoctorId, s.StartDate, s.EndDate });
    }
}
