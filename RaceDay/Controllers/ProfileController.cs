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
    public class ProfileController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ProfileController(RaceDayDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        /// <summary>
        /// Gets the profile of the logged-in user.
        /// </summary>
        /// <remarks>
        /// Available to both Organisers and Participants. Expects no request body. The profile is
        /// found from the token, so a user can only ever see their own profile. The password is
        /// never included.
        /// </remarks>
        /// <response code="200">Returns the profile, including the user's role.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="404">The account no longer exists.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProfileDto>> GetMyProfile()
        {
            int userId = GetUserId();

            if (User.IsInRole("Organiser"))
            {
                Organiser organiser = await _context.Organisers.FindAsync(userId);

                if (organiser == null)
                {
                    return NotFound();
                }

                return new ProfileDto
                {
                    Id = organiser.OrganiserCode,
                    Role = "Organiser",
                    Name = organiser.Name,
                    Surname = organiser.Surname,
                    Cell = organiser.Cell,
                    Email = organiser.Email
                };
            }

            Participant participant = await _context.Participants.FindAsync(userId);

            if (participant == null)
            {
                return NotFound();
            }

            return new ProfileDto
            {
                Id = participant.PNumber,
                Role = "Participant",
                Name = participant.Name,
                Surname = participant.Surname,
                Cell = participant.Cell,
                Email = participant.Email
            };
        }

        /// <summary>
        /// Updates the profile of the logged-in user.
        /// </summary>
        /// <remarks>
        /// Available to both Organisers and Participants. Expects a JSON body with name, surname,
        /// cell (exactly 10 digits) and email. The profile is found from the token, so a user can
        /// only ever change their own profile. The email must not belong to another account.
        /// </remarks>
        /// <param name="dto">The new profile details.</param>
        /// <response code="200">The profile was updated. Returns the updated profile.</response>
        /// <response code="400">A field is missing or invalid.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="404">The account no longer exists.</response>
        /// <response code="409">The email address already belongs to another account.</response>
        [HttpPut]
        [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ProfileDto>> UpdateMyProfile(UpdateProfileDto dto)
        {
            int userId = GetUserId();

            if (User.IsInRole("Organiser"))
            {
                Organiser organiser = await _context.Organisers.FindAsync(userId);

                if (organiser == null)
                {
                    return NotFound();
                }

                bool emailTaken = await _context.Organisers.AnyAsync(o => o.Email == dto.Email && o.OrganiserCode != userId)
                               || await _context.Participants.AnyAsync(p => p.Email == dto.Email);

                if (emailTaken)
                {
                    return Conflict("That email is already registered.");
                }

                organiser.Name = dto.Name;
                organiser.Surname = dto.Surname;
                organiser.Cell = dto.Cell;
                organiser.Email = dto.Email;

                await _context.SaveChangesAsync();

                return new ProfileDto
                {
                    Id = organiser.OrganiserCode,
                    Role = "Organiser",
                    Name = organiser.Name,
                    Surname = organiser.Surname,
                    Cell = organiser.Cell,
                    Email = organiser.Email
                };
            }

            Participant participant = await _context.Participants.FindAsync(userId);

            if (participant == null)
            {
                return NotFound();
            }

            bool participantEmailTaken = await _context.Participants.AnyAsync(p => p.Email == dto.Email && p.PNumber != userId)
                                      || await _context.Organisers.AnyAsync(o => o.Email == dto.Email);

            if (participantEmailTaken)
            {
                return Conflict("That email is already registered.");
            }

            participant.Name = dto.Name;
            participant.Surname = dto.Surname;
            participant.Cell = dto.Cell;
            participant.Email = dto.Email;

            await _context.SaveChangesAsync();

            return new ProfileDto
            {
                Id = participant.PNumber,
                Role = "Participant",
                Name = participant.Name,
                Surname = participant.Surname,
                Cell = participant.Cell,
                Email = participant.Email,
                ProfilePictureUrl = participant.ProfilePictureUrl
            };
        }
    }
}