using System.Security.Claims;
using Dappi.HeadlessCms.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Dappi.HeadlessCms.Services
{
    public class CurrentDappiSessionProvider : ICurrentDappiSessionProvider
    {
        private readonly Guid? _currentUserId;

        public CurrentDappiSessionProvider(IHttpContextAccessor accessor)
        {
            var userId = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
            {
                return;
            }

            _currentUserId = Guid.TryParse(userId, out var guid) ? guid : null;
        }

        public Guid? GetCurrentUserId() => _currentUserId;
    }
}