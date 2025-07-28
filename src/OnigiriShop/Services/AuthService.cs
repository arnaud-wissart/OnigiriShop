using OnigiriShop.Data;
using OnigiriShop.Infrastructure;
using System.Security.Claims;

namespace OnigiriShop.Services
{
    /// <summary>
    /// Fournit les méthodes liées à l'authentification et à la session utilisateur.
    /// </summary>
    public class AuthService(SessionAuthenticationStateProvider sessionAuthProvider, CartProvider cartProvider)
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
            return state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }
        public async Task<int?> GetCurrentUserIdIntAsync()
        {
            var idString = await GetCurrentUserIdAsync();
            if (int.TryParse(idString, out var id))
                return id;
            return null;
        }

        private async Task<string> GetCurrentUserNameAsync()
        {
            var state = await _sessionAuthProvider.GetAuthenticationStateAsync();
            return state.User.Identity?.Name ?? string.Empty;
        }

        public async Task<string> GetCurrentUserEmailAsync()
        {
            var state = await _sessionAuthProvider.GetAuthenticationStateAsync();
            return state.User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        }

        public async Task<string> GetCurrentUserNameOrEmailAsync()
        {
            var name = await GetCurrentUserNameAsync();
            if (name == null)
                return await GetCurrentUserEmailAsync();

            return name;
        }

        public async Task<string> GetCurrentUserRoleAsync()
        {
            var state = await _sessionAuthProvider.GetAuthenticationStateAsync();
            return state.User.FindFirst(ClaimTypes.Role)?.Value ?? AuthConstants.RoleUser;
        }

        public async Task<bool> IsAdminAsync()
        {
            return (await GetCurrentUserRoleAsync()) == AuthConstants.RoleAdmin;
        }

        public async Task LogoutAsync()
        {
            await _sessionAuthProvider.SignOutAsync();
            cartProvider.ResetMigrationFlag();
        }
    }
}
