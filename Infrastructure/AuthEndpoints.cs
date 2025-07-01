using Microsoft.AspNetCore.Authentication;
using OnigiriShop.Services;
using System.Security.Claims;

namespace OnigiriShop.Infrastructure
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            app.MapPost("/api/auth/login", async (HttpContext http, UserService userService, LoginRequest req) =>
            {
                var user = await userService.AuthenticateAsync(req.Email, req.Password);
                if (user == null)
                    return Results.Unauthorized();

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new(ClaimTypes.Name, user.Name),
                    new(ClaimTypes.Email, user.Email),
                    new(ClaimTypes.Role, user.Role)
                };
                var claimsIdentity = new ClaimsIdentity(claims, "OnigiriAuth");

                await http.SignInAsync(
                    "OnigiriAuth",
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    });

                return Results.Ok(new { user = new { user.Id, user.Name, user.Email, user.Role } });
            })
            .Accepts<LoginRequest>("application/json")
            .Produces(200)
            .Produces(401);

            app.MapPost("/api/auth/logout", async (HttpContext http) =>
            {
                await http.SignOutAsync("OnigiriAuth");
                return Results.Ok();
            });
        }
    }

    public record LoginRequest(string Email, string Password);
}
