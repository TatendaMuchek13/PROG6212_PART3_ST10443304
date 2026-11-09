namespace RaceDayApi.DTOs
{
    /// <summary>An enrolment, showing the participant, event and category it links together.</summary>
    public class EnrollmentDto
    {
        /// <summary>The id of the enrolment.</summary>
        public int EnrollID { get; set; }

        /// <summary>The id of the event.</summary>
        public int EventID { get; set; }

        /// <summary>The name of the event.</summary>
        public string EventName { get; set; }

        /// <summary>The id of the chosen category.</summary>
        public int CateID { get; set; }

        /// <summary>The name of the chosen category.</summary>
        public string CategoryName { get; set; }

        /// <summary>The id of the participant.</summary>
        public int PNumber { get; set; }

        /// <summary>The full name of the participant.</summary>
        public string ParticipantName { get; set; }

        /// <summary>The date and time the participant enrolled (UTC).</summary>
        public DateTime DateOfEnroll { get; set; }

        /// <summary>The status of the enrolment: Pending, Confirmed or Cancelled.</summary>
        public string Status { get; set; }
    }
}