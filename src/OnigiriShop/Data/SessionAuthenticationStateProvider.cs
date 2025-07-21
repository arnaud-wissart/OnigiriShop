using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using OnigiriShop.Data.Interfaces;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using System.Security.Claims;

namespace OnigiriShop.Data
{
    public class SessionAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor) : AuthenticationStateProvider, ISessionAuthenticationStateProvider
    {
        public async Task SignInAsync(User user)
        {
            ArgumentNullException.ThrowIfNull(httpContextAccessor);
            ArgumentNullException.ThrowIfNull(user);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Name ?? user.Email!),
                new(ClaimTypes.Email, user.Email!),
                new(ClaimTypes.Role, user.Role ?? AuthConstants.RoleUser),
                new(ClaimTypes.MobilePhone, user.Phone ?? string.Empty)
            };
            var identity = new ClaimsIdentity(claims, "OnigiriAuth");
            var principal = new ClaimsPrincipal(identity);

            await httpContextAccessor.HttpContext!.SignInAsync(
                "OnigiriAuth",
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public virtual async Task SignOutAsync()
        {
            if (httpContextAccessor == null)
                throw new ArgumentNullException(nameof(httpContextAccessor));

            await httpContextAccessor.HttpContext!.SignOutAsync("OnigiriAuth");
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
        }
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var user = httpContextAccessor.HttpContext?.User;
            return Task.FromResult(new AuthenticationState(user ?? new ClaimsPrincipal(new ClaimsIdentity())));
        }
    }
}
