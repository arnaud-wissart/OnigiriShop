using Microsoft.AspNetCore.Components;
using OnigiriShop.Data;
using OnigiriShop.Services;

namespace OnigiriShop.Pages
{
    public class AdminUsersBase : AdminPageBase
    {
        [Inject] public UserService UserService { get; set; }

        protected List<User> Users = new();
        protected string Search { get; set; }
        protected bool IsLoading { get; set; }

        protected override void OnInitialized()
        {
            LoadUsers();
        }

        protected void LoadUsers()
        {
            IsLoading = true;
            Users = UserService.GetAllUsers(Search);
            IsLoading = false;
        }

        protected void ToggleUserActive(User user)
        {
            UserService.SetUserActive(user.Id, !user.IsActive);
            LoadUsers();
        }

        protected async Task ResetPassword(User user)
        {
            // Génère un magic link pour ce user + envoie mail
            await UserService.GenerateAndSendResetLinkAsync(user.Email, user.Name, Nav.BaseUri);
            // Affiche toast/alerte ici
        }
    }
}
