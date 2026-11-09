using System.ComponentModel.DataAnnotations;

namespace RaceDayWeb.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Please enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }

    // The API applies the real rules (password strength, cell number, duplicate emails).
    // These checks only catch empty fields before we send anything.
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Please enter your first name.")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Please enter your surname.")]
        public string Surname { get; set; } = "";

        [Required(ErrorMessage = "Please enter your cell number.")]
        public string Cell { get; set; } = "";

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Please choose a password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The two passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";

        [Required(ErrorMessage = "Please choose how you will use RaceDay.")]
        public string Role { get; set; } = "Participant";
    }
}