using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.DTOs;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EnrollmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EnrollmentsController(RaceDayDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        // Turns enrolment rows into safe, readable response objects
        private static IQueryable<EnrollmentDto> ToDto(IQueryable<Enrollment> query)
        {
            return query.Select(e => new EnrollmentDto
            {
                EnrollID = e.EnrollID,
                EventID = e.EventID,
                EventName = e.Event.EventName,
                CateID = e.CateID,
                CategoryName = e.Category.CategoryName,
                PNumber = e.PNumber,
                ParticipantName = e.Participant.Name + " " + e.Participant.Surname,
                DateOfEnroll = e.DateOfEnroll,
                Status = e.Status

            });
        }

        /// <summary>
        /// Enters the logged-in Participant into an event under a chosen category.
        /// </summary>
        /// <remarks>
        /// Participant only. Expects a JSON body with eventID and cateID. The category must
        /// belong to the chosen event. The participant is taken from the token, not the body.
        /// A participant can enter each event only once.
        /// </remarks>
        /// <param name="dto">The event and the category to enter.</param>
        /// <response code="201">The enrolment was recorded. Returns the enrolment.</response>
        /// <response code="400">A field is invalid, or the category does not belong to the event.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not a Participant.</response>
        /// <response code="404">The event or the category does not exist.</response>
        /// <response code="409">The participant is already enrolled in this event.</response>
        [Authorize(Roles = "Participant")]
        [HttpPost]
        [ProducesResponseType(typeof(EnrollmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<EnrollmentDto>> Enroll(CreateEnrollmentDto dto)
        {
            int pNumber = GetUserId();

            Event existingEvent = await _context.Events.FindAsync(dto.EventID);

            if (existingEvent == null)
            {
                return NotFound("Event not found.");
            }

            Category category = await _context.Categories.FindAsync(dto.CateID);

            if (category == null)
            {
                return NotFound("Category not found.");
            }

            if (category.EventID != dto.EventID)
            {
                return BadRequest("That category does not belong to this event.");
            }

            bool alreadyEnrolled = await _context.Enrollments
    .AnyAsync(e => e.PNumber == pNumber && e.EventID == dto.EventID && e.Status != "Cancelled");

            if (alreadyEnrolled)
            {
                return Conflict("You are already enrolled in this event.");
            }

            Enrollment enrollment = new Enrollment
            {
                PNumber = pNumber,
                EventID = dto.EventID,
                CateID = dto.CateID,
                DateOfEnroll = DateTime.UtcNow
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            EnrollmentDto result = await ToDto(_context.Enrollments.Where(e => e.EnrollID == enrollment.EnrollID))
                .FirstAsync();

            return CreatedAtAction(nameof(GetEnrollment), new { id = enrollment.EnrollID }, result);
        }

        /// <summary>
        /// Gets the logged-in Participant's own enrolments.
        /// </summary>
        /// <remarks>Participant only. Expects no request body. Only the caller's enrolments are returned.</remarks>
        /// <response code="200">Returns the participant's enrolments. The list is empty if there are none.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not a Participant.</response>
        [Authorize(Roles = "Participant")]
        [HttpGet("my")]
        [ProducesResponseType(typeof(IEnumerable<EnrollmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<EnrollmentDto>>> GetMyEnrollments()
        {
            int pNumber = GetUserId();

            return await ToDto(_context.Enrollments.Where(e => e.PNumber == pNumber)).ToListAsync();
        }

        /// <summary>
        /// Gets the enrolments for all events that the logged-in Organiser owns.
        /// </summary>
        /// <remarks>Organiser only. Expects no request body. Enrolments for other organisers' events are never included.</remarks>
        /// <response code="200">Returns the enrolments. The list is empty if there are none.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not an Organiser.</response>
        [Authorize(Roles = "Organiser")]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<EnrollmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<EnrollmentDto>>> GetEnrollmentsForMyEvents()
        {
            int organiserCode = GetUserId();

            return await ToDto(_context.Enrollments.Where(e => e.Event.OrganiserCode == organiserCode)).ToListAsync();
        }

        /// <summary>
        /// Gets the enrolments for one event that the logged-in Organiser owns.
        /// </summary>
        /// <remarks>Organiser only, and only for their own events. The event id is taken from the address.</remarks>
        /// <param name="eventId">The id of the event.</param>
        /// <response code="200">Returns the enrolments for the event. The list is empty if there are none.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
        /// <response code="404">No event has that id.</response>
        [Authorize(Roles = "Organiser")]
        [HttpGet("event/{eventId}")]
        [ProducesResponseType(typeof(IEnumerable<EnrollmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<EnrollmentDto>>> GetEnrollmentsForEvent(int eventId)
        {
            Event existingEvent = await _context.Events.FindAsync(eventId);

            if (existingEvent == null)
            {
                return NotFound("Event not found.");
            }

            if (existingEvent.OrganiserCode != GetUserId())
            {
                return Forbid();
            }

            return await ToDto(_context.Enrollments.Where(e => e.EventID == eventId)).ToListAsync();
        }

        /// <summary>
        /// Gets a single enrolment by its id.
        /// </summary>
        /// <remarks>
        /// Allowed for the participant who made the enrolment, or the organiser who owns the
        /// event. Anyone else receives a 403. The id is taken from the address.
        /// </remarks>
        /// <param name="id">The id of the enrolment.</param>
        /// <response code="200">Returns the enrolment.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The enrolment belongs to someone else.</response>
        /// <response code="404">No enrolment has that id.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(EnrollmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EnrollmentDto>> GetEnrollment(int id)
        {
            Enrollment enrollment = await _context.Enrollments.FindAsync(id);

            if (enrollment == null)
            {
                return NotFound();
            }

            Event parentEvent = await _context.Events.FindAsync(enrollment.EventID);
            int userId = GetUserId();

            bool isEnrolledParticipant = User.IsInRole("Participant") && enrollment.PNumber == userId;
            bool isEventOrganiser = User.IsInRole("Organiser") && parentEvent.OrganiserCode == userId;

            if (!isEnrolledParticipant && !isEventOrganiser)
            {
                return Forbid();
            }

            return await ToDto(_context.Enrollments.Where(e => e.EnrollID == id)).FirstAsync();
        }

        /// <summary>
        /// Changes the status of an enrolment.
        /// </summary>
        /// <remarks>
        /// An Organiser can confirm or cancel enrolments for their own events. A Participant can only
        /// cancel their own enrolment. Expects a JSON body with the new status (Confirmed or
        /// Cancelled). A cancelled enrolment cannot be changed again, and neither can one that
        /// already has a result.
        /// </remarks>
        /// <param name="id">The id of the enrolment.</param>
        /// <param name="dto">The new status.</param>
        /// <response code="200">The status was changed. Returns the updated enrolment.</response>
        /// <response code="400">The status is not Confirmed or Cancelled.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The enrolment belongs to someone else, or a Participant tried to set anything other than Cancelled.</response>
        /// <response code="404">No enrolment has that id.</response>
        /// <response code="409">The enrolment is already cancelled or already has a result.</response>
        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(EnrollmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<EnrollmentDto>> UpdateStatus(int id, UpdateEnrollmentStatusDto dto)
        {
            Enrollment enrollment = await _context.Enrollments.FindAsync(id);

            if (enrollment == null)
            {
                return NotFound();
            }

            Event parentEvent = await _context.Events.FindAsync(enrollment.EventID);
            int userId = GetUserId();

            if (User.IsInRole("Organiser"))
            {
                if (parentEvent.OrganiserCode != userId)
                {
                    return Forbid();
                }
            }
            else
            {
                if (enrollment.PNumber != userId || dto.Status != "Cancelled")
                {
                    return Forbid();
                }
            }

            if (enrollment.Status == "Cancelled")
            {
                return Conflict("This enrolment has already been cancelled.");
            }

            bool hasResult = await _context.Results.AnyAsync(r => r.EnrollID == id);

            if (hasResult)
            {
                return Conflict("This enrolment already has a result and cannot be changed.");
            }

            enrollment.Status = dto.Status;
            await _context.SaveChangesAsync();

            return await ToDto(_context.Enrollments.Where(e => e.EnrollID == id)).FirstAsync();
        }


    }
}