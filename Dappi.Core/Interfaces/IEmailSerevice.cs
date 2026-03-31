public interface IEmailService
{
    Task<string> SendEmailAsync(
        List<string> toAddresses,
        string bodyHtml,
        string bodyText,
        string subject
    );
    bool VerifyEmailIdentityAsync(string mail);
    Task<bool> CreateEmailTemplateAsync(
        string name,
        object templateModel,
        string defaultSubjectTemplate,
        string defaultTextTemplate,
        string defaultHtmlTemplate
    );
    Task<string> SendTemplatedEmailAsync(
        List<string> toAddresses,
        string userName,
        string templateName
    );
}