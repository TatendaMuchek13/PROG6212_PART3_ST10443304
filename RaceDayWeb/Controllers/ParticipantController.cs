
using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers
{
    [SessionAuthorize(Role = "Participant")]
    public class ParticipantController : BaseController
    {
        private readonly ApiClient _api;

        public ParticipantController(ApiClient api)
        {
            _api = api;
        }

        // GET: Participant/Enrolments
        [HttpGet]
        public async Task<IActionResult> Enrolments()
        {
            var result = await _api
                .GetAsync<List<EnrollmentModel>>(
                    "api/Enrollments/my");

            var expired = HandleExpiredSession(result);

            if (expired != null)
                return expired;

            if (!result.Success)
                TempData["Error"] = result.Error;

            return View(
                result.Data ?? new List<EnrollmentModel>());
        }

        // GET: Participant/Results
        [HttpGet]
        public async Task<IActionResult> Results()
        {
            var result = await _api
                .GetAsync<List<ResultModel>>(
                    "api/Results/my");

            var expired = HandleExpiredSession(result);

            if (expired != null)
                return expired;

            if (!result.Success)
                TempData["Error"] = result.Error;

            return View(
                result.Data ?? new List<ResultModel>());
        }

        // GET: Participant/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var result = await _api
                .GetAsync<ProfileModel>("api/Profile");

            var expired = HandleExpiredSession(result);

            if (expired != null)
                return expired;

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] =
                    result.Error.Length > 0
                        ? result.Error
                        : "Unable to load profile.";

                return RedirectToAction(
                    "Index", "Events");
            }

            var model = new ProfileEditViewModel
            {
                Name = result.Data.Name,
                Surname = result.Data.Surname,
                Cell = result.Data.Cell,
                Email = result.Data.Email,
                ProfilePictureUrl =
                    result.Data.ProfilePictureUrl
            };

            return View(model);
        }

        // POST: Participant/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
            ProfileEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _api
                .PutAsync<ProfileModel>(
                    "api/Profile",
                    new
                    {
                        model.Name,
                        model.Surname,
                        model.Cell,
                        model.Email
                    });

            var expired = HandleExpiredSession(result);

            if (expired != null)
                return expired;

            if (!result.Success)
            {
                ModelState.AddModelError(
                    "", result.Error);

                return View(model);
            }

            // Upload picture if one was selected
            if (model.Picture != null &&
                model.Picture.Length > 0)
            {
                var file = model.Picture;

                var allowedTypes = new[]
                {
                    "image/jpeg",
                    "image/png",
                    "image/webp"
                };

                if (file.Length > 5 * 1024 * 1024 ||
                    !allowedTypes.Contains(file.ContentType))
                {
                    ModelState.AddModelError(
                        nameof(model.Picture),
                        "Choose a JPEG, PNG or WebP image " +
                        "smaller than 5 MB.");

                    return View(model);
                }

                var upload = await _api
                    .PostFileAsync<PictureUploadResponse>(
                        "api/Uploads/profile/picture",
                        file);

                expired = HandleExpiredSession(upload);

                if (expired != null)
                    return expired;

                if (!upload.Success)
                {
                    ModelState.AddModelError(
                        nameof(model.Picture),
                        upload.Error);

                    return View(model);
                }
            }

            TempData["Success"] =
                "Your profile was updated successfully.";

            return RedirectToAction(nameof(Profile));
        }

        private class PictureUploadResponse
        {
            public string PictureUrl { get; set; } = "";
        }
    }
}
