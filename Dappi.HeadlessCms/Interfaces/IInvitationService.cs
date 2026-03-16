using Dappi.HeadlessCms.Models;

namespace Dappi.HeadlessCms.Interfaces;

public interface IInvitationService
{
    Task<InvitationPreparationResult> PrepareInvitationAsync(InviteUserDto dto, string requestBaseUrl);

    bool TryGetInvitationPayload(string token, out InvitationPayload invitation, out string error);

    string BuildCompleteInvitationUrl(string requestBaseUrl, string token);
}
