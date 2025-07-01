using Microsoft.AspNetCore.Components;
using OnigiriShop.Data;
using OnigiriShop.Services;

namespace OnigiriShop.Pages
{
    public partial class AdminUserModal : ComponentBase
    {
        [Parameter] public bool Visible { get; set; }
        [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
        [Parameter] public EventCallback OnUserChanged { get; set; }
        [Parameter] public User UserToEdit { get; set; }
        [Parameter] public bool IsEditMode { get; set; }

        [Inject] public UserService UserService { get; set; }
        [Inject] public NavigationManager Nav { get; set; }

        protected User EditModel = new();
        protected bool IsBusy = false;
        protected string Error;

        protected bool IsAdmin
        {
            get => EditModel.Role == "Admin";
            set => EditModel.Role = value ? "Admin" : "User";
        }

        protected override void OnParametersSet()
        {
            if (UserToEdit != null)
            {
                // Copie défensive
                EditModel = new User
                {
                    Id = UserToEdit.Id,
                    Email = UserToEdit.Email,
                    Name = UserToEdit.Name,
                    Phone = UserToEdit.Phone,
                    Role = UserToEdit.Role,
                    IsActive = UserToEdit.IsActive,
                    CreatedAt = UserToEdit.CreatedAt
                };
            }
            else
            {
                EditModel = new User { Role = "User", IsActive = true };
            }
        }

        protected async Task SaveUser()
        {
            IsBusy = true;
            Error = null;

            try
            {
                if (IsEditMode)
                {
                    // Modification de l'utilisateur (tu peux faire la méthode dans UserService)
                    // À implémenter dans UserService
                    throw new NotImplementedException("Méthode de modification non implémentée");
                }
                else
                {
                    // Ajout = invitation
                    await UserService.InviteUserAsync(EditModel.Email, EditModel.Name, Nav.BaseUri);
                }

                await OnUserChanged.InvokeAsync();
                await Hide();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected async Task Hide()
        {
            await VisibleChanged.InvokeAsync(false);
        }
    }
}
