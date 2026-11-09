namespace RaceDayApi.DTOs
{
    /// <summary>A captured result, showing the participant, event and category it is for.</summary>
    public class ResultDto
    {
        /// <summary>The id of the result.</summary>
        public int ResultsID { get; set; }

        /// <summary>The id of the enrolment the result belongs to.</summary>
        public int EnrollID { get; set; }

        /// <summary>The id of the event.</summary>
        public int EventID { get; set; }

        /// <summary>The name of the event.</summary>
        public string EventName { get; set; }

        /// <summary>The date and time the event took place.</summary>
        public DateTime DateOfEvent { get; set; }

        /// <summary>The id of the category the participant entered.</summary>
        public int CateID { get; set; }

        /// <summary>The name of the category the participant entered.</summary>
        public string CategoryName { get; set; }

        /// <summary>The id of the participant.</summary>
        public int PNumber { get; set; }

        /// <summary>The full name of the participant.</summary>
        public string ParticipantName { get; set; }

        /// <summary>The outcome: Finished, DNF or DNS.</summary>
        public string ResultsStatus { get; set; }

        /// <summary>The finishing time as hours:minutes:seconds.</summary>
        public TimeSpan FinishTime { get; set; }

        /// <summary>The finishing position. 0 means the participant did not finish or start.</summary>
        public int Position { get; set; }

        /// <summary>How many participants finished in the same event and category, for example the 312 in "47th of 312".</summary>
        public int TotalFinishers { get; set; }
    }
}