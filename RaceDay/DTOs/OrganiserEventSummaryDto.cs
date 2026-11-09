namespace RaceDayApi.DTOs
{
    /// <summary>An event owned by the Organiser, with its enrolment totals, for the dashboard.</summary>
    public class OrganiserEventSummaryDto
    {
        /// <summary>The id of the event.</summary>
        public int EventID { get; set; }

        /// <summary>The name of the event.</summary>
        public string EventName { get; set; }

        /// <summary>The date and time the event starts.</summary>
        public DateTime DateOfEvent { get; set; }

        /// <summary>The type of event: Run, Walk or Cycle.</summary>
        public string EventType { get; set; }

        /// <summary>The location or route of the event.</summary>
        public string RouteInfo { get; set; }

        /// <summary>The distance in kilometres.</summary>
        public double Distance { get; set; }

        /// <summary>The address of the banner image, or null if none was uploaded.</summary>
        public string? BannerUrl { get; set; }

        /// <summary>The number of enrolments that have not been cancelled.</summary>
        public int TotalEnrolments { get; set; }

        /// <summary>The number of enrolments still waiting to be confirmed.</summary>
        public int PendingEnrolments { get; set; }
    }
}