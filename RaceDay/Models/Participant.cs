using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayApi.Models
{
    [Table ("Participate")]
    public class Participant
    {
     [Key] public int PNumber{ get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Cell { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } //This field is added for the authentication and User-based roles
        public string? ProfilePictureUrl { get; set; }
    }
}
