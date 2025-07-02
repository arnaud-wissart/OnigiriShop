using Microsoft.AspNetCore.Components;
using OnigiriShop.Data;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;

namespace OnigiriShop.Pages
{
    public class AdminUsersBase : ComponentBase
    {
        [Inject] public UserService UserService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }

        public List<User> Users { get; set; } = new();
        public bool ShowModal { get; set; }
        public bool ShowInviteModal { get; set; }
        public User ModalModel { get; set; } = new();
        public User InviteUser { get; set; }
        public bool IsBusy { get; set; }
        public bool IsEdit { get; set; }
        public string ModalTitle => IsEdit ? "Modifier l'utilisateur" : "Ajouter un utilisateur";
        public bool IsModalAdmin
        {
            get => ModalModel.Role == AuthConstants.RoleAdmin;
            set => ModalModel.Role = value ? AuthConstants.RoleAdmin : AuthConstants.RoleUser;
        }
        protected bool ShowDeleteConfirm { get; set; }
        protected User UserToDelete { get; set; }
        protected override void OnInitialized()
        {
            ReloadUsers();
        }
        protected string SearchTerm { get; set; }
        public List<User> FilteredUsers => string.IsNullOrWhiteSpace(SearchTerm)
            ? Users
            : Users.Where(u =>
                (!string.IsNullOrEmpty(u.Email) && u.Email.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrEmpty(u.Name) && u.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)))
              .ToList();

        public void ReloadUsers()
        {
            Users = UserService.GetAllUsers();
            StateHasChanged();
        }
        public void ConfirmDeleteUser(User user)
        {
            UserToDelete = user;
            ShowDeleteConfirm = true;
        }

        public void CancelDelete()
        {
            UserToDelete = null;
            ShowDeleteConfirm = false;
        }

        public void DeleteUserConfirmed()
        {
            if (UserToDelete != null)
            {
                UserService.SoftDeleteUser(UserToDelete.Id);
                ReloadUsers();
            }
            CancelDelete();
        }
        public void ShowAddModal()
        {
            ModalModel = new User { IsActive = true, Role = AuthConstants.RoleUser };
            IsEdit = false;
            ShowModal = true;
        }

        public void EditUser(User user)
        {
            // Clone pour ne pas modifier dans la liste tant que pas validé
            ModalModel = new User
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Phone = user.Phone,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive,
                Role = user.Role
            };
            IsEdit = true;
            ShowModal = true;
        }

        public void HideModal()
        {
            ShowModal = false;
            StateHasChanged();
        }

        public async Task HandleModalValid()
        {
            IsBusy = true;
            if (IsEdit)
            {
                await UserService.UpdateUserAsync(ModalModel);
            }
            else
            {
                await UserService.InviteUserAsync(ModalModel.Email, ModalModel.Name, NavigationManager.BaseUri);
            }
            IsBusy = false;
            HideModal();
            ReloadUsers();
        }

        public void ConfirmInviteUser(User user)
        {
            InviteUser = user;
            ShowInviteModal = true;
            StateHasChanged();
        }

        public void HideInviteModal()
        {
            ShowInviteModal = false;
            StateHasChanged();
        }

        public async Task SendInviteAsync()
        {
            IsBusy = true;
            await UserService.InviteUserAsync(InviteUser.Email, InviteUser.Name, NavigationManager.BaseUri);
            IsBusy = false;
            HideInviteModal();
        }
    }
}
