using System.Security.Claims;
using Dappi.HeadlessCms.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Dappi.HeadlessCms.Services
{
    public class CurrentSessionProvider : ICurrentSessionProvider
    {
        private readonly string? _currentUserId;

        public CurrentSessionProvider(IHttpContextAccessor accessor)
        {
            var userId = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
            {
                return;
            }

            _currentUserId = userId;
        }

        public string? GetCurrentUserId() => _currentUserId;
    }
}