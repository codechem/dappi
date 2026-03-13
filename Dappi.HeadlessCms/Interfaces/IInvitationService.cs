using Dappi.HeadlessCms.Models;

namespace Dappi.HeadlessCms.Interfaces;

public interface IInvitationService
{
    InvitationPreparationResult PrepareInvitation(InviteUserDto dto, string requestBaseUrl);

    bool TryGetInvitationPayload(string token, out InvitationPayload invitation, out string error);

    string BuildCompleteInvitationUrl(string requestBaseUrl, string token);
}
