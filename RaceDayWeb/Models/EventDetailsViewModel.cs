
namespace RaceDayWeb.Models
{
    public class EventDetailsViewModel
    {
        public EventModel Event { get; set; }
            = new EventModel();

        public List<CategoryModel> Categories { get; set; }
            = new List<CategoryModel>();

        public int? CateID { get; set; }
    }
}
