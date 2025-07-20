using OnigiriShop.Data;
using OnigiriShop.Services;
using System.Security.Claims;

namespace OnigiriShop.Infrastructure
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            app.MapPost("/api/auth/login", async (HttpContext http, UserAccountService accountService, SessionAuthenticationStateProvider authProvider, LoginRequest req) =>
            {
                var user = await accountService.AuthenticateAsync(req.Email, req.Password); if (user == null)
                    return Results.Unauthorized();

                await authProvider.SignInAsync(user);

                return Results.Ok(new { user = new { user.Id, user.Name, user.Email, user.Role } });
            })
            .Accepts<LoginRequest>("application/json")
            .Produces(200)
            .Produces(401);

            app.MapPost("/api/auth/logout", async (SessionAuthenticationStateProvider authProvider) =>
            {
                await authProvider.SignOutAsync();
                return Results.Ok();
            });

            app.MapPost("/api/auth/refresh", async (HttpContext http, UserService userService, SessionAuthenticationStateProvider authProvider) =>
            {
                var idStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(idStr, out var userId))
                    return Results.Unauthorized();

                var user = await userService.GetByIdAsync(userId);
                if (user == null)
                    return Results.Unauthorized();

                await authProvider.SignInAsync(user);

                return Results.Ok();
            });
        }
    }

    public record LoginRequest(string Email, string Password);
}
