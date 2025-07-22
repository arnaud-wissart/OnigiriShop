using Microsoft.AspNetCore.Components;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;

namespace OnigiriShop.Pages
{
    public partial class AdminUserModalBase : CustomComponentBase
    {
        [Parameter] public bool Visible { get; set; }
        [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
        [Parameter] public EventCallback OnUserChanged { get; set; }
        [Parameter] public User? UserToEdit { get; set; }
        [Parameter] public bool IsEditMode { get; set; }
        [Inject] public UserService UserService { get; set; } = default!;
        [Inject] public UserAccountService UserAccountService { get; set; } = default!;
        [Inject] public NavigationManager Nav { get; set; } = default!;

        protected User EditModel = new();
        protected bool IsBusy = false;
        protected string? Error;

        protected bool IsAdmin
        {
            get => EditModel.Role == AuthConstants.RoleAdmin;
            set => EditModel.Role = value ? AuthConstants.RoleAdmin : AuthConstants.RoleUser;
        }

        protected override void OnParametersSet()
        {
            if (UserToEdit != null)
            {
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
                EditModel = new User { Role = AuthConstants.RoleUser, IsActive = true };
        }

        protected async Task SaveUser()
        {
            IsBusy = true;
            Error = null;

            await HandleAsync(async () =>
            {
                if (IsEditMode)
                    await UserService.UpdateUserAsync(EditModel);
                else
                    await UserAccountService.InviteUserAsync(EditModel.Email!, EditModel.Name!, Nav.BaseUri);

                await OnUserChanged.InvokeAsync();
                await Hide();
            }, "Erreur lors de l'enregistrement de l'utilisateur");

            IsBusy = false;
        }

        protected async Task Hide() => await VisibleChanged.InvokeAsync(false);
    }
}
