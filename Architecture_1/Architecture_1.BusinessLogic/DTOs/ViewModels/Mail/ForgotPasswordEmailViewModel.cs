namespace Architecture_1.BusinessLogic.DTOs.ViewModels.Mail
{
    public class ForgotPasswordEmailViewModel
    {
        public required string Email { get; set; }
        public required string PasswordResetToken { get; set; }
        public required string ResetPasswordUrl { get; set; }
        public required string ExpiredAt { get; set; }

    }

}