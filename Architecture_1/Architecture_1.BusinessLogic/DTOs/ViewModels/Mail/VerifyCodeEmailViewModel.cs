namespace Architecture_1.BusinessLogic.DTOs.ViewModels.Mail
{
    public class VerifyCodeEmailViewModel
    {
        public required string Email { get; set; }
        public required string FullName { get; set; }
        public required string VerifyCode { get; set; }
    }
}