using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;
using System.ComponentModel.DataAnnotations;

namespace OnigiriShop.Pages
{
    public class InviteBase : CustomComponentBase
    {
        [Inject] public UserService UserService { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Parameter] public string Token { get; set; }
        protected InviteModel Model { get; set; } = new();
        protected bool Loaded { get; set; }
        protected bool TokenInvalid { get; set; }
        protected bool Success { get; set; }
        protected string Error { get; set; }
        protected bool IsBusy { get; set; }
        protected bool AlreadyActivated { get; set; }
        protected bool CanRequestNewInvite { get; set; }
        protected string UserEmailForRequest { get; set; }

        protected void GoHome() => Nav.NavigateTo("/");
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            var uri = Nav.ToAbsoluteUri(Nav.Uri);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var token = query["token"];

            // Si pas de token
            if (string.IsNullOrWhiteSpace(token))
            {
                if (await AuthService.IsAuthenticatedAsync())
                {
                    // Déjà loggué => rien à activer
                    AlreadyActivated = true;
                    Loaded = true;
                    StateHasChanged();
                    return;
                }
                TokenInvalid = true;
                Loaded = true;
                StateHasChanged();
                return;
            }

            // Vérifie le token
            var userId = await UserService.ValidateInviteTokenAsync(token);
            var user = userId > 0 ? UserService.GetAllUsers().FirstOrDefault(u => u.Id == userId) : null;

            if (userId == 0)
            {
                // On regarde si ce token est connu mais expiré pour afficher le bouton “demander un nouveau lien”
                var expiredUserId = await UserService.FindUserIdByToken(token);
                if (expiredUserId > 0)
                {
                    CanRequestNewInvite = true;
                    var expiredUser = UserService.GetAllUsers().FirstOrDefault(u => u.Id == expiredUserId);
                    UserEmailForRequest = expiredUser?.Email;
                }
                TokenInvalid = true;
                Loaded = true;
                StateHasChanged();
                return;
            }

            // Token valide mais user déjà activé
            if (user != null && user.IsActive)
            {
                AlreadyActivated = true;
                Loaded = true;
                StateHasChanged();
                return;
            }

            // Token valide, user pas activé, mais déjà loggué
            if (await AuthService.IsAuthenticatedAsync())
            {
                await AuthService.LogoutAsync();
                Nav.NavigateTo($"/invite?token={Uri.EscapeDataString(token)}", forceLoad: true);
                return;
            }

            // Cas normal
            Model.Token = token;
            UserId = userId;
            Loaded = true;
            TokenInvalid = false;
            StateHasChanged();
        }
        protected async Task RequestNewInvite()
        {
            // Envoie un mail à l’admin, ou relance un magiclink
            if (!string.IsNullOrEmpty(UserEmailForRequest))
            {
                await UserService.InviteUserAsync(UserEmailForRequest, null, Nav.BaseUri);
                // Optionnel : toast ou message pour confirmation
                await JS.InvokeVoidAsync("alert", "Un nouveau lien d'activation a été envoyé à votre adresse email.");
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
            try
            {
                await UserService.SetUserPasswordAsync(UserId, Model.Password, Model.Token);
                Success = true;
            }
            catch (Exception ex)
            {
                Error = ex.Message ?? "Erreur lors de l’activation.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public class InviteModel
        {
            public string Token { get; set; }
            [Required]
            public string Password { get; set; }
            [Required]
            [Compare(nameof(Password), ErrorMessage = "Les mots de passe ne correspondent pas.")]
            public string ConfirmPassword { get; set; }
        }
    }
}