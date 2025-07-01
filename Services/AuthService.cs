using OnigiriShop.Data;
using System.Security.Claims;

namespace OnigiriShop.Services
{
    public class AuthService(SessionAuthenticationStateProvider sessionAuthProvider)
    {
        private readonly SessionAuthenticationStateProvider _sessionAuthProvider = sessionAuthProvider
                ?? throw new InvalidOperationException("SessionAuthenticationStateProvider requis");

        public async Task<bool> IsAuthenticatedAsync()
        {
            var state = await _sessionAuthProvider.GetAuthenticationStateAsync();
            return state.User.Identity?.IsAuthenticated ?? false;
        }

        public async Task<string> GetCurrentUserIdAsync()
        {
            var state = await _sessionAuthProvider.GetAuthenticationStateAsync();
            return state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public async Task<string> GetCurrentUserNameAsync()
        {
            var state = await _sessionAuthProvider.GetAuthenticationStateAsync();
            return state.User.Identity?.Name;
        }

        public async Task<string> GetCurrentUserEmailAsync()
        {
            var state = await _sessionAuthProvider.GetAuthenticationStateAsync();
            return state.User.FindFirst(ClaimTypes.Email)?.Value;
        }

        public async Task<string> GetCurrentUserRoleAsync()
        {
            var state = await _sessionAuthProvider.GetAuthenticationStateAsync();
            return state.User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        }

        public async Task<bool> IsAdminAsync()
        {
            return (await GetCurrentUserRoleAsync()) == "Admin";
        }

        public async Task LogoutAsync()
        {
            await _sessionAuthProvider.SignOutAsync();
        }
    }
}
