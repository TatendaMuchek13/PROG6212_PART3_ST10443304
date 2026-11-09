
using System.ComponentModel.DataAnnotations;

namespace RaceDayWeb.Models
{
    public class ProfileEditViewModel
    {
        [Required(ErrorMessage = "Enter your name.")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Enter your surname.")]
        public string Surname { get; set; } = "";

        [Required(ErrorMessage = "Enter your cell number.")]
        [RegularExpression(
            @"^\d{10}$",
            ErrorMessage = "Enter a 10-digit cell number.")]
        public string Cell { get; set; } = "";

        [Required(ErrorMessage = "Enter your email.")]
        [EmailAddress]
        public string Email { get; set; } = "";

        public string? ProfilePictureUrl { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile? Picture { get; set; }
    }
}
