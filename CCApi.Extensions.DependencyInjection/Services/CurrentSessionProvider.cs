using System.Security.Claims;
using CCApi.Extensions.DependencyInjection.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CCApi.Extensions.DependencyInjection.Services
{
    public class CurrentSessionProvider : ICurrentSessionProvider
    {
        private readonly Guid? _currentUserId;

        public CurrentSessionProvider(IHttpContextAccessor accessor)
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