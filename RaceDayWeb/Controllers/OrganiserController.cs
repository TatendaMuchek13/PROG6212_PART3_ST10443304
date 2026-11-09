using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers
{
    [SessionAuthorize(Role = "Organiser")]
    public class OrganiserController : BaseController
    {
        private readonly ApiClient _api;

        public OrganiserController(ApiClient api)
        {
            _api = api;
        }

        // Loads the Organiser's own events for the event picker and finds the selected one
        private async Task<(IActionResult? Redirect, TModel Model)> LoadPicker<TModel>(int? eventId)
            where TModel : EventPickerViewModel, new()
        {
            var model = new TModel();

            var events = await _api.GetAsync<List<EventSummaryModel>>("api/Events/my");
            var expired = HandleExpiredSession(events);

            if (expired != null)
            {
                return (expired, model);
            }

            if (events.Success && events.Data != null)
            {
                model.Events = events.Data;
            }
            else
            {
                TempData["Error"] = events.Error;
            }

            if (eventId != null)
            {
                model.SelectedEvent = model.Events.FirstOrDefault(e => e.EventID == eventId);

                if (model.SelectedEvent == null)
                {
                    TempData["Error"] = "We couldn't find that event among your events.";
                }
            }

            return (null, model);
        }

        // ---------- Dashboard ----------

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var result = await _api.GetAsync<List<EventSummaryModel>>("api/Events/my");
            var expired = HandleExpiredSession(result);

            if (expired != null)
            {
                return expired;
            }

            var model = new DashboardViewModel();

            if (result.Success && result.Data != null)
            {
                model.Events = result.Data;
            }
            else
            {
                TempData["Error"] = result.Error;
            }

            return View(model);
        }

        // ---------- Events ----------

        [HttpGet]
        public async Task<IActionResult> Events()
        {
            var result = await _api.GetAsync<List<EventSummaryModel>>("api/Events/my");
            var expired = HandleExpiredSession(result);

            if (expired != null)
            {
                return expired;
            }

            if (!result.Success)
            {
                TempData["Error"] = result.Error;
            }

            return View(result.Data ?? new List<EventSummaryModel>());
        }

        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View("EventForm", new EventFormModel { DateOfEvent = DateTime.Today.AddDays(30).AddHours(7) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(EventFormModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("EventForm", model);
            }

            var create = await _api.PostAsync<EventModel>("api/Events", BuildEventBody(model));
            var expired = HandleExpiredSession(create);

            if (expired != null)
            {
                return expired;
            }

            if (!create.Success || create.Data == null)
            {
                ModelState.AddModelError(string.Empty, create.Error);
                return View("EventForm", model);
            }

            string? bannerError = await UploadBanner(create.Data.EventID, model.Banner);
            SetSaveMessage(bannerError);

            return RedirectToAction(nameof(Events));
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var mine = await _api.GetAsync<List<EventSummaryModel>>("api/Events/my");
            var expired = HandleExpiredSession(mine);

            if (expired != null)
            {
                return expired;
            }

            if (!mine.Success || mine.Data == null || !mine.Data.Any(e => e.EventID == id))
            {
                TempData["Error"] = "We couldn't find that event among your events.";
                return RedirectToAction(nameof(Events));
            }

            var ev = await _api.GetAsync<EventModel>("api/Events/" + id);

            if (!ev.Success || ev.Data == null)
            {
                TempData["Error"] = ev.Error;
                return RedirectToAction(nameof(Events));
            }

            var form = new EventFormModel
            {
                EventID = ev.Data.EventID,
                EventName = ev.Data.EventName,
                Description = ev.Data.Description,
                DateOfEvent = ev.Data.DateOfEvent,
                Location = ev.Data.RouteInfo,
                Distance = ev.Data.Distance,
                EventType = ev.Data.EventType,
                BannerUrl = ev.Data.BannerUrl
            };

            return View("EventForm", form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(int id, EventFormModel model)
        {
            model.EventID = id;

            if (!ModelState.IsValid)
            {
                return View("EventForm", model);
            }

            var update = await _api.PutAsync<EventModel>("api/Events/" + id, BuildEventBody(model));
            var expired = HandleExpiredSession(update);

            if (expired != null)
            {
                return expired;
            }

            if (!update.Success)
            {
                ModelState.AddModelError(string.Empty, update.Error);
                return View("EventForm", model);
            }

            string? bannerError = await UploadBanner(id, model.Banner);
            SetSaveMessage(bannerError);

            return RedirectToAction(nameof(Events));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var result = await _api.DeleteAsync<string>("api/Events/" + id);
            var expired = HandleExpiredSession(result);

            if (expired != null)
            {
                return expired;
            }

            if (result.Success)
            {
                TempData["Success"] = "The event has been deleted.";
            }
            else
            {
                TempData["Error"] = result.Error;
            }

            return RedirectToAction(nameof(Events));
        }

        private static object BuildEventBody(EventFormModel model)
        {
            return new
            {
                model.EventName,
                model.Description,
                DateOfEvent = model.DateOfEvent!.Value,
                RouteInfo = model.Location,
                Distance = model.Distance!.Value,
                model.EventType
            };
        }

        // Returns null on success or when no file was chosen, otherwise the reason it failed
        private async Task<string?> UploadBanner(int eventId, IFormFile? banner)
        {
            if (banner == null || banner.Length == 0)
            {
                return null;
            }

            var upload = await _api.PostFileAsync<string>("api/Uploads/events/" + eventId + "/banner", banner);

            return upload.Success ? null : upload.Error;
        }

        private void SetSaveMessage(string? bannerError)
        {
            if (bannerError == null)
            {
                TempData["Success"] = "The event has been saved.";
            }
            else
            {
                TempData["Info"] = "The event was saved, but the banner image could not be uploaded: " + bannerError;
            }
        }

        // ---------- Categories ----------

        [HttpGet]
        public async Task<IActionResult> Categories(int? eventId)
        {
            var (redirect, model) = await LoadPicker<CategoriesViewModel>(eventId);

            if (redirect != null)
            {
                return redirect;
            }

            if (model.SelectedEvent != null)
            {
                var categories = await _api.GetAsync<List<CategoryModel>>("api/Categories/event/" + model.SelectedEvent.EventID);
                var expired = HandleExpiredSession(categories);

                if (expired != null)
                {
                    return expired;
                }

                if (categories.Success && categories.Data != null)
                {
                    model.Categories = categories.Data;
                }
                else
                {
                    TempData["Error"] = categories.Error;
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(int eventId, string? categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["Error"] = "Please enter a category name.";
                return RedirectToAction(nameof(Categories), new { eventId });
            }

            var result = await _api.PostAsync<CategoryModel>("api/Categories", new { eventId, categoryName = categoryName.Trim() });
            var expired = HandleExpiredSession(result);

            if (expired != null)
            {
                return expired;
            }

            if (result.Success)
            {
                TempData["Success"] = "Category added.";
            }
            else
            {
                TempData["Error"] = result.Error;
            }

            return RedirectToAction(nameof(Categories), new { eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id, int eventId)
        {
            var result = await _api.DeleteAsync<string>("api/Categories/" + id);
            var expired = HandleExpiredSession(result);

            if (expired != null)
            {
                return expired;
            }

            if (result.Success)
            {
                TempData["Success"] = "Category deleted.";
            }
            else
            {
                TempData["Error"] = result.Error;
            }

            return RedirectToAction(nameof(Categories), new { eventId });
        }

        // ---------- Enrolments ----------

        [HttpGet]
        public async Task<IActionResult> Enrolments(int? eventId)
        {
            var (redirect, model) = await LoadPicker<EnrolmentsViewModel>(eventId);

            if (redirect != null)
            {
                return redirect;
            }

            if (model.SelectedEvent != null)
            {
                var enrolments = await _api.GetAsync<List<EnrollmentModel>>("api/Enrollments/event/" + model.SelectedEvent.EventID);
                var expired = HandleExpiredSession(enrolments);

                if (expired != null)
                {
                    return expired;
                }

                if (enrolments.Success && enrolments.Data != null)
                {
                    model.Enrolments = enrolments.Data;
                }
                else
                {
                    TempData["Error"] = enrolments.Error;
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEnrolmentStatus(int id, int eventId, string status)
        {
            var result = await _api.PutAsync<EnrollmentModel>("api/Enrollments/" + id + "/status", new { status });
            var expired = HandleExpiredSession(result);

            if (expired != null)
            {
                return expired;
            }

            if (result.Success)
            {
                TempData["Success"] = "The enrolment is now " + status.ToLower() + ".";
            }
            else
            {
                TempData["Error"] = result.Error;
            }

            return RedirectToAction(nameof(Enrolments), new { eventId });
        }

        // ---------- Results ----------

        [HttpGet]
        public async Task<IActionResult> Results(int? eventId)
        {
            var (redirect, model) = await LoadPicker<ResultsViewModel>(eventId);

            if (redirect != null)
            {
                return redirect;
            }

            if (model.SelectedEvent != null)
            {
                var enrolments = await _api.GetAsync<List<EnrollmentModel>>("api/Enrollments/event/" + model.SelectedEvent.EventID);
                var results = await _api.GetAsync<List<ResultModel>>("api/Results/event/" + model.SelectedEvent.EventID);

                var expired = HandleExpiredSession(enrolments) ?? HandleExpiredSession(results);

                if (expired != null)
                {
                    return expired;
                }

                if (enrolments.Success && results.Success && enrolments.Data != null && results.Data != null)
                {
                    model.Rows = enrolments.Data
                        .Where(e => e.Status != "Cancelled")
                        .OrderBy(e => e.ParticipantName)
                        .Select(e => new ResultRowModel
                        {
                            Enrollment = e,
                            Result = results.Data.FirstOrDefault(r => r.EnrollID == e.EnrollID)
                        })
                        .ToList();
                }
                else
                {
                    TempData["Error"] = enrolments.Success ? results.Error : enrolments.Error;
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveResult(ResultFormModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please check the finish time and position you entered.";
                return RedirectToAction("Results", new { eventId = model.EventID });
            }

            if (model.ResultsStatus == "Finished" && model.FinishTime == null)
            {
                TempData["Error"] = "Please enter the finish time for a finisher.";
                return RedirectToAction("Results", new { eventId = model.EventID });
            }

            TimeSpan finishTime = model.FinishTime ?? TimeSpan.Zero;

            ApiResult<ResultModel> result;

            if (model.ResultsID > 0)
            {
                result = await _api.PutAsync<ResultModel>("api/Results/" + model.ResultsID,
                    new { model.ResultsStatus, FinishTime = finishTime, model.Position });
            }
            else
            {
                result = await _api.PostAsync<ResultModel>("api/Results",
                    new { model.EnrollID, model.ResultsStatus, FinishTime = finishTime, model.Position });
            }

            var expired = HandleExpiredSession(result);

            if (expired != null)
            {
                return expired;
            }

            if (result.Success)
            {
                TempData["Success"] = "The result has been saved.";
            }
            else
            {
                TempData["Error"] = result.Error;
            }

            return RedirectToAction("Results", new { eventId = model.EventID });
        }
    }
}