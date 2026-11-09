using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayApi.Models
{
    public class Enrollment
    {
        [Key] public int EnrollID { get; set; }

        public int PNumber { get; set; }
        [ForeignKey("PNumber")]
        public Participant? Participant { get; set; }

        public int EventID { get; set; }
        [ForeignKey("EventID")]
        public Event? Event { get; set; }

        public int CateID { get; set; }
        [ForeignKey("CateID")]
        public Category? Category { get; set; }

        public DateTime DateOfEnroll { get; set; }

        public string Status { get; set; } = "Pending";     // Pending, Confirmed or Cancelled
    }
}