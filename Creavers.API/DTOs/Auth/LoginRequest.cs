namespace Creavers.API.DTOs.Auth
{
    public class LoginRequest
    {
        /// <summary>Can be either an Email address or a Phone number.</summary>
        public string EmailOrPhone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
