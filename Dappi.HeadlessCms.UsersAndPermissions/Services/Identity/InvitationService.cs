using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dappi.HeadlessCms.UsersAndPermissions.Api;
using Dappi.HeadlessCms.UsersAndPermissions.Core;
using Dappi.HeadlessCms.UsersAndPermissions.Interfaces;
using Dappi.HeadlessCms.UsersAndPermissions.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace Dappi.HeadlessCms.UsersAndPermissions.Services.Identity;

public class InvitationService(
    IConfiguration configuration,
    IDataProtectionProvider dataProtectionProvider,
    IEmailService? emailService = null
) : IInvitationService
{
    private const string DefaultInvitationEmailSubjectTemplate = "You're invited to join Dappi";
    private const string DefaultInvitationEmailHtmlTemplatePath = "Invitation/InvitationEmail.html";
    private const string DefaultInvitationEmailTextTemplatePath = "Invitation/InvitationEmail.txt";

    private readonly IDataProtector _invitationProtector = dataProtectionProvider.CreateProtector(
        "Dappi.HeadlessCms.UsersAndPermissions.Invitation.v1"
    );

    public async Task<InvitationPreparationResult> PrepareInvitationAsync(
        InviteUserDto dto,
        string requestBaseUrl
    )
    {
        var defaultInvitationEmailHtmlTemplate = ReadTemplateFile(
            DefaultInvitationEmailHtmlTemplatePath,
            "<p>Hi {{ username }},</p><p>You've been invited to join Dappi.</p><p><strong>Temporary password:</strong> {{ temporary_password }}</p><p><a href=\"{{ accept_url }}\">Accept invitation</a></p><p>This link expires in {{ expiry_hours }} hours.</p>"
        );
        var defaultInvitationEmailTextTemplate = ReadTemplateFile(
            DefaultInvitationEmailTextTemplatePath,
            "Hi {{ username }},\n\nYou've been invited to join Dappi.\nTemporary password: {{ temporary_password }}\n\nAccept your invitation here:\n{{ accept_url }}\n\nThis link expires in {{ expiry_hours }} hours."
        );

        var rolesToAssign = dto.Roles.Count > 0
            ? dto.Roles
            : [UsersAndPermissionsConstants.DefaultRoles.Authenticated];
        var generatedPassword = string.IsNullOrWhiteSpace(dto.Password)
            ? GeneratePassword()
            : dto.Password!;

        var invitationPayload = new InvitationPayload(
            dto.Username,
            dto.Email,
            generatedPassword,
            rolesToAssign,
            DateTime.UtcNow.AddDays(2)
        );

        var protectedToken = _invitationProtector.Protect(JsonSerializer.Serialize(invitationPayload));
        var token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(protectedToken));

        var acceptancePath = $"/api/usersandpermissions/accept-invitation?token={token}";
        var acceptUrl = $"{requestBaseUrl}{acceptancePath}";

        var frontendUrl = configuration.GetValue<string>("Dappi:FrontendUrl");
        var frontendAcceptUrl = string.IsNullOrWhiteSpace(frontendUrl)
            ? null
            : $"{frontendUrl.TrimEnd('/')}/accept-invitation?token={token}";

        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["username"] = dto.Username,
            ["temporary_password"] = generatedPassword,
            ["accept_url"] = acceptUrl,
            ["expiry_hours"] = "48",
        };

        var configuredSubjectTemplate = configuration.GetValue<string>(
            "Dappi:InvitationEmailSubjectTemplate"
        );
        var configuredTextTemplate = configuration.GetValue<string>(
            "Dappi:InvitationEmailTextTemplate"
        );
        var configuredHtmlTemplate = configuration.GetValue<string>(
            "Dappi:InvitationEmailHtmlTemplate"
        );

        var emailSubject = RenderTemplate(
            configuredSubjectTemplate,
            DefaultInvitationEmailSubjectTemplate,
            replacements
        );
        var emailTextBody = RenderTemplate(
            configuredTextTemplate,
            defaultInvitationEmailTextTemplate,
            replacements
        );
        var emailHtmlBody = RenderTemplate(
            configuredHtmlTemplate,
            defaultInvitationEmailHtmlTemplate,
            replacements
        );

        if (emailService is not null)
        {
            var reusableTemplateModel = new
            {
                username = "{{username}}",
                temporary_password = "{{temporary_password}}",
                accept_url = "{{accept_url}}",
                expiry_hours = "{{expiry_hours}}",
            };

            await emailService.CreateEmailTemplateAsync(
                "InviteUser",
                reusableTemplateModel,
                DefaultInvitationEmailSubjectTemplate,
                defaultInvitationEmailTextTemplate,
                defaultInvitationEmailHtmlTemplate
            );
        }

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
        catch
        {
            invitation = default!;
            error = "Invitation token is invalid.";
            return false;
        }
    }

    public string BuildCompleteInvitationUrl(string requestBaseUrl, string token)
    {
        var frontendUrl = configuration.GetValue<string>("Dappi:FrontendUrl");
        var baseUrl = string.IsNullOrWhiteSpace(frontendUrl)
            ? requestBaseUrl.TrimEnd('/')
            : frontendUrl.TrimEnd('/');

        return $"{baseUrl}/complete-invitation?token={token}&flow=usersandpermissions";
    }

    private static string RenderTemplate(
        string? configuredTemplate,
        string fallbackTemplate,
        IReadOnlyDictionary<string, string> replacements
    )
    {
        var template = string.IsNullOrWhiteSpace(configuredTemplate)
            ? fallbackTemplate
            : configuredTemplate;

        foreach (var (key, value) in replacements)
        {
            template = template.Replace($"{{{{ {key} }}}}", value, StringComparison.OrdinalIgnoreCase);
            template = template.Replace($"{{{{{key}}}}}", value, StringComparison.OrdinalIgnoreCase);
        }

        return template;
    }

    private static string ReadTemplateFile(string relativePath, string fallbackTemplate)
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, relativePath);
        return File.Exists(templatePath) ? File.ReadAllText(templatePath) : fallbackTemplate;
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
