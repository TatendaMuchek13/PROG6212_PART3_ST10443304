
using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers
{
    [SessionAuthorize(Role = "Participant")]
    public class EventsController : BaseController
    {
        private readonly ApiClient _api;

        public EventsController(ApiClient api)
        {
            _api = api;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? eventType, string? search)
        {
            var query = new List<string>
            {
                "upcomingOnly=true"
            };

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                query.Add("eventType=" +
                    Uri.EscapeDataString(eventType));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query.Add("search=" +
                    Uri.EscapeDataString(search));
            }

            var url = "api/Events?" +
                string.Join("&", query);

            var result = await _api.GetAsync<List<EventModel>>(url);

            var expired = HandleExpiredSession(result);

            if (expired != null)
                return expired;

            if (!result.Success)
                TempData["Error"] = result.Error;

            ViewBag.EventType = eventType;
            ViewBag.Search = search;

            return View(result.Data ?? new List<EventModel>());
        }
    }
}
