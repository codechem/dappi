using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace Dappi.HeadlessCms.Services.Identity;

public class InvitationService : IInvitationService
{
    private readonly IConfiguration _configuration;
    private readonly IDataProtector _invitationProtector;

    public InvitationService(IConfiguration configuration, IDataProtectionProvider dataProtectionProvider)
    {
        _configuration = configuration;
        _invitationProtector = dataProtectionProvider.CreateProtector("Dappi.HeadlessCms.Users.Invitation.v1");
    }

    public InvitationPreparationResult PrepareInvitation(InviteUserDto dto, string requestBaseUrl)
    {
        var rolesToAssign = dto.Roles.Count > 0 ? dto.Roles : new List<string> { Constants.UserRoles.User };
        var generatedPassword = GeneratePassword();

        var invitationPayload = new InvitationPayload(
            dto.Username,
            dto.Email,
            generatedPassword,
            rolesToAssign,
            DateTime.UtcNow.AddDays(2)
        );

        var protectedToken = _invitationProtector.Protect(JsonSerializer.Serialize(invitationPayload));
        var token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(protectedToken));

        var acceptancePath = $"/api/users/accept-invitation?token={token}";
        var acceptUrl = $"{requestBaseUrl}{acceptancePath}";

        var frontendUrl = _configuration.GetValue<string>(Constants.Configuration.FrontendUrl);
        var frontendAcceptUrl = string.IsNullOrWhiteSpace(frontendUrl)
            ? null
            : $"{frontendUrl.TrimEnd('/')}/accept-invitation?token={token}";

        var emailSubject = "You're invited to join Dappi";
        var emailTextBody =
            $"Hi {dto.Username},\n\nYou've been invited to join Dappi.\nTemporary password: {generatedPassword}\n\nAccept your invitation here:\n{acceptUrl}\n\nThis link expires in 48 hours.";
        var emailHtmlBody =
            $"<p>Hi {dto.Username},</p><p>You've been invited to join Dappi.</p><p><strong>Temporary password:</strong> {generatedPassword}</p><p><a href=\"{acceptUrl}\">Accept invitation</a></p><p>This link expires in 48 hours.</p>";

        return new InvitationPreparationResult(
            token,
            generatedPassword,
            acceptUrl,
            frontendAcceptUrl,
            emailSubject,
            emailTextBody,
            emailHtmlBody
        );
    }

    public bool TryGetInvitationPayload(string token, out InvitationPayload invitation, out string error)
    {
        try
        {
            var decodedTokenBytes = WebEncoders.Base64UrlDecode(token);
            var protectedToken = Encoding.UTF8.GetString(decodedTokenBytes);
            var unprotectedPayload = _invitationProtector.Unprotect(protectedToken);
            var parsedInvitation = JsonSerializer.Deserialize<InvitationPayload>(unprotectedPayload);

            if (parsedInvitation is null)
            {
                invitation = default!;
                error = "Invitation payload is invalid.";
                return false;
            }

            invitation = parsedInvitation;
            error = string.Empty;
            return true;
        }
        catch (Exception)
        {
            invitation = default!;
            error = "Invitation token is invalid.";
            return false;
        }
    }

    public string BuildCompleteInvitationUrl(string requestBaseUrl, string token)
    {
        return $"{requestBaseUrl}/complete-invitation?token={token}";
    }

    private static string GeneratePassword()
    {
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string symbols = "!@#$%^&*()_-+=[]{}<>?";

        var requiredCharacters = new List<char>
        {
            GetRandomCharacter(lower),
            GetRandomCharacter(upper),
            GetRandomCharacter(digits),
            GetRandomCharacter(symbols),
        };

        var allCharacters = lower + upper + digits + symbols;
        const int totalLength = 12;

        while (requiredCharacters.Count < totalLength)
        {
            requiredCharacters.Add(GetRandomCharacter(allCharacters));
        }

        for (var index = requiredCharacters.Count - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (requiredCharacters[index], requiredCharacters[swapIndex]) =
                (requiredCharacters[swapIndex], requiredCharacters[index]);
        }

        return new string(requiredCharacters.ToArray());
    }

    private static char GetRandomCharacter(string source)
    {
        var position = RandomNumberGenerator.GetInt32(source.Length);
        return source[position];
    }
}
