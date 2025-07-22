using Microsoft.AspNetCore.Components;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;
using System.ComponentModel.DataAnnotations;

namespace OnigiriShop.Pages
{
    public partial class AdminInviteUserBase : CustomComponentBase
    {
        [Inject] public NavigationManager Nav { get; set; } = default!;
        [Inject] public UserAccountService UserAccountService { get; set; } = default!;
        protected string Name { get; set; } = string.Empty;
        protected string Email { get; set; } = string.Empty;
        protected string Message { get; set; } = string.Empty;
        protected bool IsBusy { get; set; }
        protected InviteUserModel Model { get; set; } = new();
        protected async Task InviteAsync()
        {
            Message = string.Empty;
            IsBusy = true;
            await HandleAsync(async () =>
            {
                var baseUrl = Nav.BaseUri;
                await UserAccountService.InviteUserAsync(Model.Email.Trim(), Model.Name.Trim(), baseUrl);
                Message = "Invitation envoyée !";
                Model = new InviteUserModel();
            }, "Erreur lors de l'invitation");
            IsBusy = false;
        }
    }

    public class InviteUserModel
    {
        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Format d'email invalide.")]
        public string Email { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }
}
