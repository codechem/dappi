using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Scriban;

namespace Dappi.HeadlessCms.Services.Identity;

public class InvitationService : IInvitationService
{
    private const string DefaultInvitationEmailSubjectTemplate = "You're invited to join Dappi";
    private const string DefaultInvitationEmailTextTemplate =
        "Hi {{ username }},\n\nYou've been invited to join Dappi.\nTemporary password: {{ temporary_password }}\n\nAccept your invitation here:\n{{ accept_url }}\n\nThis link expires in {{ expiry_hours }} hours.";
    private const string DefaultInvitationEmailHtmlTemplate =
        "<p>Hi {{ username }},</p><p>You've been invited to join Dappi.</p><p><strong>Temporary password:</strong> {{ temporary_password }}</p><p><a href=\"{{ accept_url }}\">Accept invitation</a></p><p>This link expires in {{ expiry_hours }} hours.</p>";

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

        var templateModel = new
        {
            username = dto.Username,
            temporary_password = generatedPassword,
            accept_url = acceptUrl,
            expiry_hours = 48,
        };

        var emailSubjectTemplate = _configuration.GetValue<string>(Constants.Configuration.InvitationEmailSubjectTemplate);
        var emailTextTemplate = _configuration.GetValue<string>(Constants.Configuration.InvitationEmailTextTemplate);
        var emailHtmlTemplate = _configuration.GetValue<string>(Constants.Configuration.InvitationEmailHtmlTemplate);

        var emailSubject = RenderTemplate(
            emailSubjectTemplate,
            DefaultInvitationEmailSubjectTemplate,
            templateModel
        );
        var emailTextBody = RenderTemplate(
            emailTextTemplate,
            DefaultInvitationEmailTextTemplate,
            templateModel
        );
        var emailHtmlBody = RenderTemplate(
            emailHtmlTemplate,
            DefaultInvitationEmailHtmlTemplate,
            templateModel
        );

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

    private static string RenderTemplate(string? configuredTemplate, string fallbackTemplate, object model)
    {
        var templateToUse = string.IsNullOrWhiteSpace(configuredTemplate)
            ? fallbackTemplate
            : configuredTemplate;

        var parsedTemplate = Template.Parse(templateToUse);

        return parsedTemplate.HasErrors
            ? Template.Parse(fallbackTemplate).Render(model)
            : parsedTemplate.Render(model);
    }
}
