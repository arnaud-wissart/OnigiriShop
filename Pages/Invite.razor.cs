using Microsoft.AspNetCore.Components;
using OnigiriShop.Services;

namespace OnigiriShop.Pages
{
    public partial class Invite : ComponentBase
    {
        [Inject] public UserService UserService { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Parameter][SupplyParameterFromQuery(Name = "token")] public string Token { get; set; }
        protected string Password { get; set; }
        protected string PasswordConfirm { get; set; }
        protected string Message { get; set; }
        protected bool IsBusy { get; set; }
        protected bool IsValidToken { get; set; }
        protected int UserId { get; set; }

        protected override async Task OnInitializedAsync()
        {
            if (string.IsNullOrWhiteSpace(Token))
            {
                Message = "Lien invalide.";
                IsValidToken = false;
                return;
            }

            // Vérifie le token et récupère UserId
            var userId = await UserService.ValidateInviteTokenAsync(Token);
            if (userId > 0)
            {
                UserId = userId;
                IsValidToken = true;
            }
            else
            {
                Message = "Lien d’invitation expiré ou invalide.";
                IsValidToken = false;
            }
        }

        protected async Task SubmitAsync()
        {
            Message = "";
            if (Password != PasswordConfirm)
            {
                Message = "Les mots de passe ne correspondent pas.";
                return;
            }
            if (Password.Length < 8)
            {
                Message = "Le mot de passe doit contenir au moins 8 caractères.";
                return;
            }
            IsBusy = true;
            try
            {
                await UserService.SetUserPasswordAsync(UserId, Password, Token);
                Message = "Mot de passe enregistré. Votre compte est activé !";
                // Redirige ou propose la connexion selon ta logique
            }
            catch (Exception ex)
            {
                Message = "Erreur : " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
