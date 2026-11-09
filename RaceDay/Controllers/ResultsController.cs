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
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        private IQueryable<ResultDto> ToDto(IQueryable<Result> query)
        {
            return query.Select(r => new ResultDto
            {
                ResultsID = r.ResultsID,
                EnrollID = r.EnrollID,
                EventID = r.Enrollment.EventID,
                EventName = r.Enrollment.Event.EventName,
                DateOfEvent = r.Enrollment.Event.DateOfEvent,
                CateID = r.CateID,
                CategoryName = r.Category.CategoryName,
                PNumber = r.Enrollment.PNumber,
                ParticipantName = r.Enrollment.Participant.Name + " " + r.Enrollment.Participant.Surname,
                ResultsStatus = r.ResultsStatus,
                FinishTime = r.FinishTime,
                Position = r.Position,
                TotalFinishers = _context.Results.Count(x =>
                    x.Enrollment.EventID == r.Enrollment.EventID &&
                    x.CateID == r.CateID &&
                    x.ResultsStatus == "Finished")
            });
        }

        // Returns an error message if the status and position don't make sense together
        private string CheckResultRules(UpdateResultDto dto)
        {
            if (dto.ResultsStatus == "Finished" && dto.Position < 1)
            {
                return "A finished participant needs a position of 1 or higher.";
            }

            if (dto.ResultsStatus != "Finished" && dto.Position != 0)
            {
                return "Only finished participants can have a position.";
            }

            return null;
        }

        // Is this position already taken by someone else in the same event and category?
        private async Task<bool> PositionTaken(Enrollment enrollment, int position, int ignoreResultId)
        {
            if (position == 0)
            {
                return false;
            }

            return await _context.Results.AnyAsync(r =>
                r.ResultsID != ignoreResultId &&
                r.Enrollment.EventID == enrollment.EventID &&
                r.CateID == enrollment.CateID &&
                r.Position == position);
        }

        /// <summary>
        /// Captures the result of a participant's enrolment after an event.
        /// </summary>
        /// <remarks>
        /// Organiser only, and only for their own events. Expects a JSON body with enrollID,
        /// resultsStatus (Finished, DNF or DNS), finishTime (for example 00:42:35) and position.
        /// A finished participant needs a position of 1 or higher. Anyone who did not finish
        /// or did not start must have a position of 0. Each enrolment can have only one result,
        /// and a position can be used only once per category of an event.
        /// </remarks>
        /// <param name="dto">The enrolment and the result details.</param>
        /// <response code="201">The result was captured. Returns the result.</response>
        /// <response code="400">A field is invalid, or the status and position do not match.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
        /// <response code="404">No enrolment has that id.</response>
        /// <response code="409">The enrolment already has a result, or the position is already taken.</response>
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ResultDto>> CaptureResult(CreateResultDto dto)
        {
            Enrollment enrollment = await _context.Enrollments.FindAsync(dto.EnrollID);

            if (enrollment == null)
            {
                return NotFound("Enrolment not found.");
            }

            Event parentEvent = await _context.Events.FindAsync(enrollment.EventID);

            if (parentEvent.OrganiserCode != GetUserId())
            {
                return Forbid();
            }

            string ruleError = CheckResultRules(dto);

            if (ruleError != null)
            {
                return BadRequest(ruleError);
            }

            bool resultExists = await _context.Results.AnyAsync(r => r.EnrollID == dto.EnrollID);

            if (resultExists)
            {
                return Conflict("A result has already been captured for this enrolment.");
            }

            if (await PositionTaken(enrollment, dto.Position, 0))
            {
                return Conflict("That position is already taken in this category.");
            }

            Result result = new Result
            {
                EnrollID = dto.EnrollID,
                CateID = enrollment.CateID,
                ResultsStatus = dto.ResultsStatus,
                FinishTime = dto.FinishTime,
                Position = dto.Position
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            ResultDto created = await ToDto(_context.Results.Where(r => r.ResultsID == result.ResultsID)).FirstAsync();

            return CreatedAtAction(nameof(GetResult), new { id = result.ResultsID }, created);
        }

        /// <summary>
        /// Updates a result that was already captured.
        /// </summary>
        /// <remarks>
        /// Organiser only, and only for their own events. Expects a JSON body with resultsStatus
        /// (Finished, DNF or DNS), finishTime and position, under the same rules as capturing a
        /// result. The result id is taken from the address.
        /// </remarks>
        /// <param name="id">The id of the result to update.</param>
        /// <param name="dto">The new result details.</param>
        /// <response code="200">The result was updated. Returns the updated result.</response>
        /// <response code="400">A field is invalid, or the status and position do not match.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
        /// <response code="404">No result has that id.</response>
        /// <response code="409">The position is already taken in this category.</response>
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ResultDto>> UpdateResult(int id, UpdateResultDto dto)
        {
            Result result = await _context.Results.FindAsync(id);

            if (result == null)
            {
                return NotFound();
            }

            Enrollment enrollment = await _context.Enrollments.FindAsync(result.EnrollID);
            Event parentEvent = await _context.Events.FindAsync(enrollment.EventID);

            if (parentEvent.OrganiserCode != GetUserId())
            {
                return Forbid();
            }

            string ruleError = CheckResultRules(dto);

            if (ruleError != null)
            {
                return BadRequest(ruleError);
            }

            if (await PositionTaken(enrollment, dto.Position, id))
            {
                return Conflict("That position is already taken in this category.");
            }

            result.ResultsStatus = dto.ResultsStatus;
            result.FinishTime = dto.FinishTime;
            result.Position = dto.Position;

            await _context.SaveChangesAsync();

            return await ToDto(_context.Results.Where(r => r.ResultsID == id)).FirstAsync();
        }

        /// <summary>
        /// Gets the logged-in Participant's own results.
        /// </summary>
        /// <remarks>Participant only. Expects no request body. Only the caller's results are returned.</remarks>
        /// <response code="200">Returns the participant's results. The list is empty if there are none.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not a Participant.</response>
        [Authorize(Roles = "Participant")]
        [HttpGet("my")]
        [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<ResultDto>>> GetMyResults()
        {
            int pNumber = GetUserId();

            return await ToDto(_context.Results.Where(r => r.Enrollment.PNumber == pNumber)).ToListAsync();
        }

        /// <summary>
        /// Gets all results for one event that the logged-in Organiser owns.
        /// </summary>
        /// <remarks>Organiser only, and only for their own events. The event id is taken from the address.</remarks>
        /// <param name="eventId">The id of the event.</param>
        /// <response code="200">Returns the results for the event. The list is empty if there are none.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
        /// <response code="404">No event has that id.</response>
        [Authorize(Roles = "Organiser")]
        [HttpGet("event/{eventId}")]
        [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<ResultDto>>> GetResultsForEvent(int eventId)
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

            return await ToDto(_context.Results.Where(r => r.Enrollment.EventID == eventId)).ToListAsync();
        }

        /// <summary>
        /// Gets a single result by its id.
        /// </summary>
        /// <remarks>
        /// Allowed for the participant the result belongs to, or the organiser who owns the
        /// event. Anyone else receives a 403. The id is taken from the address.
        /// </remarks>
        /// <param name="id">The id of the result.</param>
        /// <response code="200">Returns the result.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The result belongs to someone else.</response>
        /// <response code="404">No result has that id.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResultDto>> GetResult(int id)
        {
            Result result = await _context.Results.FindAsync(id);

            if (result == null)
            {
                return NotFound();
            }

            Enrollment enrollment = await _context.Enrollments.FindAsync(result.EnrollID);
            Event parentEvent = await _context.Events.FindAsync(enrollment.EventID);
            int userId = GetUserId();

            bool isOwnResult = User.IsInRole("Participant") && enrollment.PNumber == userId;
            bool isEventOrganiser = User.IsInRole("Organiser") && parentEvent.OrganiserCode == userId;

            if (!isOwnResult && !isEventOrganiser)
            {
                return Forbid();
            }

            return await ToDto(_context.Results.Where(r => r.ResultsID == id)).FirstAsync();
        }
    }
}