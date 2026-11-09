using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.DTOs
{
    /// <summary>The new status for an enrolment.</summary>
    public class UpdateEnrollmentStatusDto
    {
        /// <summary>The new status. Must be exactly Confirmed or Cancelled.</summary>
        [Required]
        [RegularExpression("^(Confirmed|Cancelled)$", ErrorMessage = "Status must be Confirmed or Cancelled.")]
        public string Status { get; set; }
    }
}