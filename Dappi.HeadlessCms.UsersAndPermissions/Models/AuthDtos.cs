namespace Dappi.HeadlessCms.UsersAndPermissions.Models;

public sealed record InvitationPreparationResult(
    string Token,
    string GeneratedPassword,
    string AcceptUrl,
    string? FrontendAcceptUrl,
    string EmailSubject,
    string EmailTextBody,
    string EmailHtmlBody
);

public class CompleteInvitationDto
{
    public required string Token { get; set; }
    public required string OldPassword { get; set; }
    public required string NewPassword { get; set; }
}

public class AcceptInvitationQueryDto
{
    public string Token { get; set; } = string.Empty;
}

public sealed record InvitationPayload(
    string Username,
    string Email,
    string Password,
    List<string> Roles,
    DateTime ExpiresAtUtc
);