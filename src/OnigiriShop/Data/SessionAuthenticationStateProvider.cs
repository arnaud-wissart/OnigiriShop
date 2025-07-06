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
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? AuthConstants.RoleUser),
                new Claim(ClaimTypes.MobilePhone, user.Phone ?? string.Empty)
            };
            var identity = new ClaimsIdentity(claims, "OnigiriAuth");
            var principal = new ClaimsPrincipal(identity);

            await httpContextAccessor.HttpContext.SignInAsync(
                "OnigiriAuth",
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task SignOutAsync()
        {
            await httpContextAccessor.HttpContext.SignOutAsync("OnigiriAuth");
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
        }
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var user = httpContextAccessor.HttpContext?.User;
            return Task.FromResult(new AuthenticationState(user ?? new ClaimsPrincipal(new ClaimsIdentity())));
        }
    }
}
