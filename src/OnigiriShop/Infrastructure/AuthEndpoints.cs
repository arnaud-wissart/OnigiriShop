using Microsoft.AspNetCore.Identity.Data;
using OnigiriShop.Data;
using OnigiriShop.Services;
using System.Net;
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

            app.MapPost("/api/auth/forgot-password", async (
                HttpContext http,
                UserAccountService accountService,
                ForgotPasswordRequest req) =>
            {
                if (string.IsNullOrWhiteSpace(req.Email))
                    return Results.BadRequest(new { error = "Email requis." });

                var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
                try
                {
                    await accountService.GenerateAndSendResetLinkAsync(req.Email, req.Email, baseUrl);
                }
                catch
                {
                    // On renvoie toujours OK pour ne pas divulguer l'existence du compte
                }

                return Results.Ok();
            })
            .Accepts<ForgotPasswordRequest>("application/json")
            .Produces(200)
            .Produces(400);

            app.MapGet("/reset-login", async (
                HttpContext http,
                UserAccountService accountService,
                UserService userService,
                SessionAuthenticationStateProvider authProvider) =>
            {
                var token = http.Request.Query["token"].ToString();
                if (string.IsNullOrWhiteSpace(token))
                    return Results.Redirect("/");

                var userId = await accountService.ValidateInviteTokenAsync(token);
                if (userId == 0)
                    return Results.Redirect("/");

                var user = await userService.GetByIdAsync(userId);
                if (user == null)
                    return Results.Redirect("/");

                await authProvider.SignInAsync(user);
                await accountService.MarkTokenUsedAsync(token);

                return Results.Redirect("/profile");
            });
        }
    }

    public record LoginRequest(string Email, string Password);
    public record MagicLinkRequest(string Token);
    public record AccessRequest(string Email, string Message);
    public record ForgotPasswordRequest(string Email);
}
