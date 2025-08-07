using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OnigiriShop.Data;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;
using System.ComponentModel.DataAnnotations;

namespace OnigiriShop.Pages
{
    public class InviteBase : CustomComponentBase
    {
        [Inject] public UserService UserService { get; set; } = default!;
        [Inject] public UserAccountService UserAccountService { get; set; } = default!;
        [Inject] public NavigationManager Nav { get; set; } = default!;
        [Inject] public SessionAuthenticationStateProvider SessionAuthProvider { get; set; } = default!;
        [Inject] public HttpClient Http { get; set; } = default!;
        [Inject] public ToastService ToastService { get; set; } = default!;
        [Parameter] public string Token { get; set; } = string.Empty;
        protected InviteModel Model { get; set; } = new();
        protected bool Loaded { get; set; }
        protected bool TokenInvalid { get; set; }
        protected string? Error { get; set; } = string.Empty;
        protected bool IsBusy { get; set; }
        protected bool CanRequestNewInvite { get; set; }
        protected string UserEmailForRequest { get; set; } = string.Empty;
        protected int UserIdForRequest { get; set; }
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            var uri = Nav.ToAbsoluteUri(Nav.Uri);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var token = query["token"];

            if (string.IsNullOrWhiteSpace(token))
            {
                if (await AuthService.IsAuthenticatedAsync())
                {
                    Nav.NavigateTo("/profile", forceLoad: true);
                    return;
                }
                TokenInvalid = true;
                Loaded = true;
                StateHasChanged();
                return;
            }

            var userId = await UserAccountService.ValidateInviteTokenAsync(token);
            var user = userId > 0 ? await UserService.GetByIdAsync(userId) : null;
            if (userId == 0)
            {
                var expiredUserId = await UserAccountService.FindUserIdByTokenAsync(token);
                if (expiredUserId > 0)
                {
                    CanRequestNewInvite = true;
                    var expiredUser = await UserService.GetByIdAsync(expiredUserId);
                    UserEmailForRequest = expiredUser?.Email!;
                    UserIdForRequest = expiredUserId;
                }
                TokenInvalid = true;
                Loaded = true;
                StateHasChanged();
                return;
            }

            if (user != null && user.IsActive)
            {
                await Http.PostAsJsonAsync("/api/auth/magic-link", new { token });
                Nav.NavigateTo("/profile", forceLoad: true);
                return;
            }

            if (await AuthService.IsAuthenticatedAsync())
            {
                await AuthService.LogoutAsync();
                Nav.NavigateTo($"/invite?token={Uri.EscapeDataString(token)}", forceLoad: true);
                return;
            }

            Model.Token = token;
            UserId = userId;
            Loaded = true;
            TokenInvalid = false;
            StateHasChanged();
        }
        protected async Task RequestNewInvite()
        {
            if (UserIdForRequest > 0)
            {
                await HandleAsync(async () =>
                {
                    await UserAccountService.ResendInvitationAsync(UserIdForRequest, Nav.BaseUri);
                    ToastService.ShowToast("Invitation renvoyée avec succès !", string.Empty, ToastLevel.Success);
                }, "Erreur lors de l'envoi de l'invitation");
            }
        }

        protected async Task SubmitPassword()
        {
            Error = null;
            if (string.IsNullOrWhiteSpace(Model.Password) || Model.Password.Length < 6)
            {
                Error = "Le mot de passe doit contenir au moins 6 caractères.";
                return;
            }
            if (Model.Password != Model.ConfirmPassword)
            {
                Error = "Les mots de passe ne correspondent pas.";
                return;
            }
            IsBusy = true;
            await HandleAsync(async () =>
            {
                await UserAccountService.SetUserPasswordAsync(UserId, Model.Password, Model.Token);
                var user = await UserService.GetByIdAsync(UserId);
                if (user != null)
                {
                    await SessionAuthProvider.SignInAsync(user);
                    Nav.NavigateTo("/profile", forceLoad: true);
                }
            }, "Erreur lors de l’activation");
            IsBusy = false;
        }

        public class InviteModel
        {
            public string Token { get; set; } = string.Empty;
            [Required]
            public string Password { get; set; } = string.Empty;
            [Required]
            [Compare(nameof(Password), ErrorMessage = "Les mots de passe ne correspondent pas.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }
    }
}
