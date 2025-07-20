using Microsoft.AspNetCore.Components;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;
using System.ComponentModel.DataAnnotations;

namespace OnigiriShop.Pages
{
    public partial class AdminInviteUserBase : CustomComponentBase
    {
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public UserAccountService UserAccountService { get; set; }
        protected string Name { get; set; }
        protected string Email { get; set; }
        protected string Message { get; set; }
        protected bool IsBusy { get; set; }
        protected InviteUserModel Model { get; set; } = new();
        protected async Task InviteAsync()
        {
            Message = "";
            IsBusy = true;
            try
            {
                var baseUrl = Nav.BaseUri;
                await UserAccountService.InviteUserAsync(Model.Email.Trim(), Model.Name?.Trim(), baseUrl);
                Message = "Invitation envoyée !";
                Model = new InviteUserModel();
            }
            catch (Exception ex)
            {
                Message = $"Erreur lors de l'invitation : {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public class InviteUserModel
    {
        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Format d'email invalide.")]
        public string Email { get; set; }

        public string Name { get; set; }
    }
}
