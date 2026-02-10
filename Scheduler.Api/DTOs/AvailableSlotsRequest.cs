using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Scheduler.Api.DTOs;

public class AvailableSlotsRequest
{
    [DefaultValue("2025-09-15")]
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    
    [DefaultValue(17)]
    [Required(ErrorMessage = "Specialization ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Specialization ID must be greater than 0")]
    public int SpecializationId { get; set; }
    
    public int? DoctorId { get; set; }

    [DefaultValue(30)]
    [Required(ErrorMessage = "Slot duration is required")]
    [Range(1, 480, ErrorMessage = "Slot duration must be between 1 and 480 minutes")]
    public int SlotDurationMinutes { get; set; }

    [DefaultValue(100)]
    [Required(ErrorMessage = "Maximum results is required")]
    [Range(1, 1000, ErrorMessage = "Maximum results must be between 1 and 1000")]
    public int MaxResults { get; set; }
}
