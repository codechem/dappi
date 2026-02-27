namespace Dappi.HeadlessCms.Interfaces
{
    public interface IEmailService
    {
        Task<string> SendEmailAsync(
            List<string> toAddresses,
            string bodyHtml,
            string bodyText,
            string subject
        );
        Task<bool> VerifyEmailIdentityAsync(string mail);
        Task<bool> CreateEmailTemplateAsync(string name, string subject, string text, string html);
        Task<string> SendTemplatedEmailAsync(string toAddress, string userName);
    }
}
