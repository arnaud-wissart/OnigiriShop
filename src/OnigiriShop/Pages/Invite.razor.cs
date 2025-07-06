using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OnigiriShop.Services;
using System.ComponentModel.DataAnnotations;

namespace OnigiriShop.Pages
{
    public class InviteBase : ComponentBase
    {
        [Inject] public UserService UserService { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public IJSRuntime JS { get; set; }
        [Inject] public AuthService AuthService { get; set; }
        [Parameter] public string Token { get; set; }

        protected InviteModel Model { get; set; } = new();
        protected bool Loaded { get; set; }
        protected bool TokenInvalid { get; set; }
        protected bool Success { get; set; }
        protected string Error { get; set; }
        protected bool IsBusy { get; set; }
        protected int UserId { get; set; }
        protected void GoHome() => Nav.NavigateTo("/");
        protected override async Task OnInitializedAsync()
        {
            // S’il y a un utilisateur loggé, déconnexion directe
            if (await AuthService.IsAuthenticatedAsync())
            {
                await AuthService.LogoutAsync();
                Nav.NavigateTo(Nav.Uri, forceLoad: true);
                return;
            }

            var uri = Nav.ToAbsoluteUri(Nav.Uri);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            Model.Token = query["token"];

            if (string.IsNullOrWhiteSpace(Model.Token))
            {
                TokenInvalid = true;
                Loaded = true;
                StateHasChanged();
                return;
            }

            UserId = await UserService.ValidateInviteTokenAsync(Model.Token);
            Loaded = true;
            TokenInvalid = UserId == 0;
            StateHasChanged();
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