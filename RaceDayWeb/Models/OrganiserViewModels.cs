using System.ComponentModel.DataAnnotations;

namespace RaceDayWeb.Models
{
    public class DashboardViewModel
    {
        public List<EventSummaryModel> Events { get; set; } = new();

        public int TotalEvents => Events.Count;
        public int TotalEnrolments => Events.Sum(e => e.TotalEnrolments);
        public int PendingEnrolments => Events.Sum(e => e.PendingEnrolments);

        public List<EventSummaryModel> Upcoming => Events.Where(e => e.DateOfEvent >= DateTime.Now).OrderBy(e => e.DateOfEvent).ToList();
        public List<EventSummaryModel> Past => Events.Where(e => e.DateOfEvent < DateTime.Now).OrderByDescending(e => e.DateOfEvent).ToList();
        public List<EventSummaryModel> Ordered => Upcoming.Concat(Past).ToList();
        public EventSummaryModel? NextEvent => Upcoming.FirstOrDefault();
    }

    public class EventFormModel
    {
        public int EventID { get; set; }

        [Required(ErrorMessage = "Please enter the event name.")]
        [StringLength(100, ErrorMessage = "The event name must be 100 characters or fewer.")]
        [Display(Name = "Event name")]
        public string EventName { get; set; } = "";

        [Required(ErrorMessage = "Please describe the event.")]
        public string Description { get; set; } = "";

        [Required(ErrorMessage = "Please choose the date and time of the event.")]
        [Display(Name = "Date and time")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? DateOfEvent { get; set; }

        [Required(ErrorMessage = "Please enter the location.")]
        public string Location { get; set; } = "";

        [Required(ErrorMessage = "Please enter the distance in kilometres.")]
        [Range(0.1, 1000, ErrorMessage = "The distance must be between 0.1 and 1000 km.")]
        [Display(Name = "Distance (km)")]
        public double? Distance { get; set; }

        [Required(ErrorMessage = "Please choose the event type.")]
        [Display(Name = "Event type")]
        public string EventType { get; set; } = "";

        public string? BannerUrl { get; set; }

        [Display(Name = "Banner image")]
        public IFormFile? Banner { get; set; }
    }

    public class EventPickerViewModel
    {
        public List<EventSummaryModel> Events { get; set; } = new();
        public EventSummaryModel? SelectedEvent { get; set; }
    }

    public class CategoriesViewModel : EventPickerViewModel
    {
        public List<CategoryModel> Categories { get; set; } = new();
    }

    public class EnrolmentsViewModel : EventPickerViewModel
    {
        public List<EnrollmentModel> Enrolments { get; set; } = new();
    }

    public class ResultRowModel
    {
        public EnrollmentModel Enrollment { get; set; } = new();
        public ResultModel? Result { get; set; }
    }

    public class ResultsViewModel : EventPickerViewModel
    {
        public List<ResultRowModel> Rows { get; set; } = new();
    }

    public class ResultFormModel
    {
        public int EventID { get; set; }
        public int EnrollID { get; set; }
        public int ResultsID { get; set; }
        public string ResultsStatus { get; set; } = "Finished";
        public TimeSpan? FinishTime { get; set; }
        public int Position { get; set; }
    }
}