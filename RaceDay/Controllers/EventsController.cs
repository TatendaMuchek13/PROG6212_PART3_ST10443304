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
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EventsController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets events, with optional filters.
        /// </summary>
        /// <remarks>
        /// Available to any logged-in user. Expects no request body. All filters are optional
        /// and can be combined: upcomingOnly (true returns only events that have not started),
        /// eventType (Run, Walk or Cycle), fromDate and toDate (inclusive, for example 2026-12-01),
        /// and search (matches part of the event name or the location). Results are sorted by date.
        /// </remarks>
        /// <param name="upcomingOnly">True to return only events that have not started yet.</param>
        /// <param name="eventType">Run, Walk or Cycle.</param>
        /// <param name="fromDate">Only events on or after this date.</param>
        /// <param name="toDate">Only events on or before this date.</param>
        /// <param name="search">Text to look for in the event name or location.</param>
        /// <response code="200">Returns the matching events. The list is empty if none match.</response>
        /// <response code="401">The user is not logged in.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Event>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents(
            bool upcomingOnly = false,
            string? eventType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? search = null)
        {
            IQueryable<Event> query = _context.Events;

            if (upcomingOnly)
            {
                query = query.Where(e => e.DateOfEvent >= DateTime.Now);
            }

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                query = query.Where(e => e.EventType == eventType);
            }

            if (fromDate != null)
            {
                query = query.Where(e => e.DateOfEvent >= fromDate.Value.Date);
            }

            if (toDate != null)
            {
                query = query.Where(e => e.DateOfEvent < toDate.Value.Date.AddDays(1));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e => e.EventName.Contains(search) || e.RouteInfo.Contains(search));
            }

            return await query.OrderBy(e => e.DateOfEvent).ToListAsync();
        }

        /// <summary>
        /// Gets the events owned by the logged-in Organiser, with enrolment totals.
        /// </summary>
        /// <remarks>
        /// Organiser only. Expects no request body. Used for the dashboard: each event shows how
        /// many enrolments it has (cancelled ones are not counted) and how many are still pending.
        /// Events are sorted by date.
        /// </remarks>
        /// <response code="200">Returns the Organiser's events. The list is empty if there are none.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not an Organiser.</response>
        [Authorize(Roles = "Organiser")]
        [HttpGet("my")]
        [ProducesResponseType(typeof(IEnumerable<OrganiserEventSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<OrganiserEventSummaryDto>>> GetMyEvents()
        {
            int organiserCode = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            return await _context.Events
                .Where(e => e.OrganiserCode == organiserCode)
                .OrderBy(e => e.DateOfEvent)
                .Select(e => new OrganiserEventSummaryDto
                {
                    EventID = e.EventID,
                    EventName = e.EventName,
                    DateOfEvent = e.DateOfEvent,
                    EventType = e.EventType,
                    RouteInfo = e.RouteInfo,
                    Distance = e.Distance,
                    BannerUrl = e.BannerUrl,
                    TotalEnrolments = _context.Enrollments.Count(en => en.EventID == e.EventID && en.Status != "Cancelled"),
                    PendingEnrolments = _context.Enrollments.Count(en => en.EventID == e.EventID && en.Status == "Pending")
                })
                .ToListAsync();
        }
       

        /// <summary>
        /// Gets a single event by its id.
        /// </summary>
        /// <remarks>Available to any logged-in user. The id is taken from the address.</remarks>
        /// <param name="id">The id of the event.</param>
        /// <response code="200">Returns the event.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="404">No event has that id.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Event>> GetEvent(int id)
        {
            Event existingEvent = await _context.Events.FindAsync(id);

            if (existingEvent == null)
            {
                return NotFound();
            }

            return existingEvent;
        }

        /// <summary>
        /// Creates a new event owned by the logged-in Organiser.
        /// </summary>
        /// <remarks>
        /// Organiser only. Expects a JSON body with eventName, description, dateOfEvent,
        /// routeInfo (the location), distance (in km, between 0.1 and 1000) and eventType
        /// (Run, Walk or Cycle). The organiser is taken from the token, not the body.
        /// </remarks>
        /// <param name="dto">The details of the new event.</param>
        /// <response code="201">The event was created. Returns the new event.</response>
        /// <response code="400">A field is missing or invalid.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not an Organiser.</response>
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        [ProducesResponseType(typeof(Event), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<Event>> CreateEvent(EventDto dto)
        {
            int organiserCode = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            Event newEvent = new Event
            {
                OrganiserCode = organiserCode,
                EventName = dto.EventName,
                Description = dto.Description,
                DateOfEvent = dto.DateOfEvent,
                RouteInfo = dto.RouteInfo,
                Distance = dto.Distance,
                EventType = dto.EventType
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = newEvent.EventID }, newEvent);
        }

        /// <summary>
        /// Updates an event that the logged-in Organiser owns.
        /// </summary>
        /// <remarks>
        /// Organiser only, and only for their own events. Expects the same JSON body as creating
        /// an event. The event id is taken from the address.
        /// </remarks>
        /// <param name="id">The id of the event to update.</param>
        /// <param name="dto">The new details of the event.</param>
        /// <response code="200">The event was updated. Returns the updated event.</response>
        /// <response code="400">A field is missing or invalid.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
        /// <response code="404">No event has that id.</response>
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(int id, EventDto dto)
        {
            Event existingEvent = await _context.Events.FindAsync(id);

            if (existingEvent == null)
            {
                return NotFound();
            }

            int organiserCode = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (existingEvent.OrganiserCode != organiserCode)
            {
                return Forbid();
            }

            existingEvent.EventName = dto.EventName;
            existingEvent.Description = dto.Description;
            existingEvent.DateOfEvent = dto.DateOfEvent;
            existingEvent.RouteInfo = dto.RouteInfo;
            existingEvent.Distance = dto.Distance;
            existingEvent.EventType = dto.EventType;

            await _context.SaveChangesAsync();

            return Ok(existingEvent);
        }

        /// <summary>
        /// Deletes an event that the logged-in Organiser owns.
        /// </summary>
        /// <remarks>
        /// Organiser only, and only for their own events. Expects no request body. An event that
        /// still has categories or enrolments cannot be deleted.
        /// </remarks>
        /// <param name="id">The id of the event to delete.</param>
        /// <response code="200">The event was deleted. Returns a confirmation message.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
        /// <response code="404">No event has that id.</response>
        /// <response code="409">The event still has categories or enrolments.</response>
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            Event existingEvent = await _context.Events.FindAsync(id);

            if (existingEvent == null)
            {
                return NotFound();
            }

            int organiserCode = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (existingEvent.OrganiserCode != organiserCode)
            {
                return Forbid();
            }

            bool hasChildren = await _context.Categories.AnyAsync(c => c.EventID == id)
                            || await _context.Enrollments.AnyAsync(e => e.EventID == id);

            if (hasChildren)
            {
                return Conflict("This event has categories or enrolments and cannot be deleted.");
            }

            _context.Events.Remove(existingEvent);
            await _context.SaveChangesAsync();

            return Ok("Event " + existingEvent.EventName + " has been deleted.");
        }
    }
}