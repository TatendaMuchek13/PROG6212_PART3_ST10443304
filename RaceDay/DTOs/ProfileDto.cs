namespace RaceDayApi.DTOs
{
    /// <summary>The profile of the logged-in user. The password is never included.</summary>
    public class ProfileDto
    {
        /// <summary>The user's id: the participant number or the organiser code.</summary>
        public int Id { get; set; }

        /// <summary>The account type: Organiser or Participant.</summary>
        public string Role { get; set; }

        /// <summary>First name.</summary>
        public string Name { get; set; }

        /// <summary>Surname.</summary>
        public string Surname { get; set; }

        /// <summary>Cell number.</summary>
        public string Cell { get; set; }

        /// <summary>Email address.</summary>
        public string Email { get; set; }

        /// <summary>The address of the profile picture, or null if none was uploaded. Participants only.</summary>
        public string? ProfilePictureUrl { get; set; }
    }
}