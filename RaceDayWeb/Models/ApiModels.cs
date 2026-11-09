namespace RaceDayWeb.Models
{
    // Shapes of the replies we get back from the API
    public class LoginResponse
    {
        public string Token { get; set; } = "";
        public string Role { get; set; } = "";
    }

    public class ProfileModel
    {
        public int Id { get; set; }
        public string Role { get; set; } = "";
        public string Name { get; set; } = "";
        public string Surname { get; set; } = "";
        public string Cell { get; set; } = "";
        public string Email { get; set; } = "";
        public string? ProfilePictureUrl { get; set; }
    }
}