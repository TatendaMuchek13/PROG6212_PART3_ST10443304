namespace RaceDayWeb.Models
{
    public class EventModel
    {
        public int EventID { get; set; }
        public int OrganiserCode { get; set; }
        public string EventName { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime DateOfEvent { get; set; }
        public string RouteInfo { get; set; } = "";
        public double Distance { get; set; }
        public string EventType { get; set; } = "";
        public string? BannerUrl { get; set; }
    }

    public class EventSummaryModel
    {
        public int EventID { get; set; }
        public string EventName { get; set; } = "";
        public DateTime DateOfEvent { get; set; }
        public string EventType { get; set; } = "";
        public string RouteInfo { get; set; } = "";
        public double Distance { get; set; }
        public string? BannerUrl { get; set; }
        public int TotalEnrolments { get; set; }
        public int PendingEnrolments { get; set; }
    }

    public class CategoryModel
    {
        public int CateID { get; set; }
        public int EventID { get; set; }
        public string CategoryName { get; set; } = "";
    }

    public class EnrollmentModel
    {
        public int EnrollID { get; set; }
        public int EventID { get; set; }
        public string EventName { get; set; } = "";
        public int CateID { get; set; }
        public string CategoryName { get; set; } = "";
        public int PNumber { get; set; }
        public string ParticipantName { get; set; } = "";
        public DateTime DateOfEnroll { get; set; }
        public string Status { get; set; } = "";
    }

    public class ResultModel
    {
        public int ResultsID { get; set; }
        public int EnrollID { get; set; }
        public int EventID { get; set; }
        public string EventName { get; set; } = "";
        public DateTime DateOfEvent { get; set; }
        public int CateID { get; set; }
        public string CategoryName { get; set; } = "";
        public int PNumber { get; set; }
        public string ParticipantName { get; set; } = "";
        public string ResultsStatus { get; set; } = "";
        public TimeSpan FinishTime { get; set; }
        public int Position { get; set; }
        public int TotalFinishers { get; set; }
    }
}