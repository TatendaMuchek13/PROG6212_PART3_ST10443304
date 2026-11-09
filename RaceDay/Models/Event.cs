using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayApi.Models
{
    /// <summary>An event that participants can enter.</summary>
    [Table("Events")]
    public class Event
    {
        /// <summary>The id of the event.</summary>
        [Key]
        public int EventID { get; set; }

        /// <summary>The code of the Organiser who owns the event.</summary>
        public int OrganiserCode { get; set; }

        [ForeignKey("OrganiserCode")]
        public Organiser? Organiser { get; set; }

        /// <summary>The name of the event.</summary>
        public string EventName { get; set; }

        /// <summary>A short description of the event.</summary>
        public string Description { get; set; }

        /// <summary>The date and time the event starts.</summary>
        public DateTime DateOfEvent { get; set; }

        /// <summary>The location or route of the event.</summary>
        public string RouteInfo { get; set; }

        /// <summary>The distance in kilometres.</summary>
        public double Distance { get; set; }

        /// <summary>The type of event: Run, Walk or Cycle.</summary>
        public string EventType { get; set; }
       


        public string? BannerUrl { get; set; }
    }
}