using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDayApi.Data;
using RaceDayApi.Models;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UploadsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly IImageService _images;

        public UploadsController(RaceDayDbContext context, IImageService images)
        {
            _context = context;
            _images = images;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        /// <summary>
        /// Uploads a banner image for an event that the logged-in Organiser owns.
        /// </summary>
        /// <remarks>
        /// Organiser only, and only for their own events. Expects a multipart form with a single
        /// image file (JPEG, PNG or WebP, up to 5 MB). The image is stored in Azure Blob Storage
        /// and its address is saved on the event, replacing any earlier banner.
        /// </remarks>
        /// <param name="id">The id of the event.</param>
        /// <param name="file">The banner image.</param>
        /// <response code="200">The banner was uploaded. Returns its address as bannerUrl.</response>
        /// <response code="400">No file was sent, the file is too large, or it is not a JPEG, PNG or WebP image.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
        /// <response code="404">No event has that id.</response>
        [Authorize(Roles = "Organiser")]
        [HttpPost("events/{id}/banner")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadEventBanner(int id, IFormFile file)
        {
            Event existingEvent = await _context.Events.FindAsync(id);

            if (existingEvent == null)
            {
                return NotFound();
            }

            if (existingEvent.OrganiserCode != GetUserId())
            {
                return Forbid();
            }

            string error = _images.Validate(file);

            if (error != null)
            {
                return BadRequest(error);
            }

            string url = await _images.UploadAsync(file, "events");

            existingEvent.BannerUrl = url;
            await _context.SaveChangesAsync();

            return Ok(new { bannerUrl = url });
        }

        /// <summary>
        /// Uploads a profile picture for the logged-in Participant.
        /// </summary>
        /// <remarks>
        /// Participant only. Expects a multipart form with a single image file (JPEG, PNG or WebP,
        /// up to 5 MB). The image is stored in Azure Blob Storage and its address is saved on the
        /// participant's profile, replacing any earlier picture.
        /// </remarks>
        /// <param name="file">The profile picture.</param>
        /// <response code="200">The picture was uploaded. Returns its address as pictureUrl.</response>
        /// <response code="400">No file was sent, the file is too large, or it is not a JPEG, PNG or WebP image.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not a Participant.</response>
        /// <response code="404">The account no longer exists.</response>
        [Authorize(Roles = "Participant")]
        [HttpPost("profile/picture")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            Participant participant = await _context.Participants.FindAsync(GetUserId());

            if (participant == null)
            {
                return NotFound();
            }

            string error = _images.Validate(file);

            if (error != null)
            {
                return BadRequest(error);
            }

            string url = await _images.UploadAsync(file, "profiles");

            participant.ProfilePictureUrl = url;
            await _context.SaveChangesAsync();

            return Ok(new { pictureUrl = url });
        }
    }
}