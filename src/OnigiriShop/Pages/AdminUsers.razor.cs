using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;
using System.ComponentModel.DataAnnotations;

namespace OnigiriShop.Pages
{
    public class AdminUsersBase : ComponentBase, IDisposable
    {
        [Inject] public UserService UserService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }
        [Inject] public IJSRuntime JS { get; set; }
        [Inject] public ToastService ToastService { get; set; }
        public List<User> Users { get; set; } = new();
        public bool ShowModal { get; set; }
        public bool ShowInviteModal { get; set; }
        public UserInputModel ModalModel { get; set; } = new();
        public int ModalModelId { get; set; }
        public User InviteUser { get; set; }
        public bool IsBusy { get; set; }
        public bool IsEdit { get; set; }
        public string ModalTitle => IsEdit ? "Modifier l'utilisateur" : "Ajouter un utilisateur";
        public bool IsModalAdmin
        {
            get => ModalModel.Role == AuthConstants.RoleAdmin;
            set => ModalModel.Role = value ? AuthConstants.RoleAdmin : AuthConstants.RoleUser;
        }
        protected Guid _editFormKey = Guid.NewGuid();

        protected bool ShowDeleteConfirm { get; set; }
        protected User UserToDelete { get; set; }
        protected EditContext _editContext;
        private ValidationMessageStore _messageStore;
        protected override Task OnInitializedAsync()
        {
            _editContext = new EditContext(ModalModel);
            _messageStore = new ValidationMessageStore(_editContext);
            _editContext.OnFieldChanged += EditContext_OnFieldChanged;
            ReloadUsers();
            return Task.CompletedTask;
        }
        private void EditContext_OnFieldChanged(object sender, FieldChangedEventArgs e)
        {
            _messageStore.Clear(e.FieldIdentifier);
            _editContext.NotifyValidationStateChanged();
        }
        protected string SearchTerm { get; set; } = "";

        public List<User> FilteredUsers =>
            string.IsNullOrWhiteSpace(SearchTerm)
            ? Users
            : Users.Where(u =>
                (!string.IsNullOrEmpty(u.Email) && u.Email.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrEmpty(u.Name) && u.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrEmpty(u.Phone) && u.Phone.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        public void Dispose()
        {
            if (_editContext != null)
                _editContext.OnFieldChanged -= EditContext_OnFieldChanged;
        }
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
            ModalModel.Email = "";
            ModalModel.Name = "";
            ModalModel.Phone = "";
            ModalModel.IsActive = true;
            ModalModel.Role = AuthConstants.RoleUser;
            ModalModelId = 0;
            IsEdit = false;
            _messageStore.Clear();
            _editContext.NotifyValidationStateChanged();
            _editFormKey = Guid.NewGuid();
            ShowModal = true;
        }

        public void EditUser(User user)
        {
            ModalModel.Email = user.Email;
            ModalModel.Name = user.Name;
            ModalModel.Phone = user.Phone;
            ModalModel.IsActive = user.IsActive;
            ModalModel.Role = user.Role;
            ModalModelId = user.Id;
            IsEdit = true;
            _messageStore.Clear();
            _editContext.NotifyValidationStateChanged();
            _editFormKey = Guid.NewGuid();
            ShowModal = true;
        }

        public void HideModal()
        {
            ShowModal = false;
            _messageStore.Clear();
            _editContext.NotifyValidationStateChanged();
        }

        public string AlertMessage { get; set; }
        public string AlertCss { get; set; }
        public bool ShowAlert { get; set; }
        private async Task FocusFieldAsync(string fieldName)
        {
            await JS.InvokeVoidAsync("focusElementByName", fieldName);
        }
        private bool HasValidationError(string fieldName)
        {
            var fieldIdentifier = new FieldIdentifier(ModalModel, fieldName);
            return _messageStore[fieldIdentifier]?.Any() == true;
        }
        public async Task HandleModalValid()
        {
            IsBusy = true;
            _messageStore.Clear();
            _editContext.NotifyValidationStateChanged();
            await InvokeAsync(StateHasChanged);

            ShowAlert = false;
            bool hasError = false;

            if (string.IsNullOrWhiteSpace(ModalModel.Email) || !ModalModel.Email.Contains("@") || !ModalModel.Email.Contains("."))
            {
                _messageStore.Add(() => ModalModel.Email, "Veuillez saisir un email valide.");
                hasError = true;
            }
            if (string.IsNullOrWhiteSpace(ModalModel.Name))
            {
                _messageStore.Add(() => ModalModel.Name, "Le nom est requis.");
                hasError = true;
            }
            if (Users.Any(u => u.Email.Equals(ModalModel.Email, StringComparison.OrdinalIgnoreCase) && (!IsEdit || u.Id != ModalModelId)))
            {
                _messageStore.Add(() => ModalModel.Email, "Cet email est déjà utilisé.");
                hasError = true;
            }
            if (Users.Any(u => u.Name.Equals(ModalModel.Name, StringComparison.OrdinalIgnoreCase) && (!IsEdit || u.Id != ModalModelId)))
            {
                _messageStore.Add(() => ModalModel.Name, "Ce nom est déjà utilisé.");
                hasError = true;
            }

            _editContext.NotifyValidationStateChanged();
            await InvokeAsync(StateHasChanged);

            if (hasError)
            {
                IsBusy = false;
                if (HasValidationError(nameof(ModalModel.Email)))
                    await FocusFieldAsync("ModalModel.Email");
                else if (HasValidationError(nameof(ModalModel.Name)))
                    await FocusFieldAsync("ModalModel.Name");
                return;
            }

            try
            {
                if (IsEdit)
                {
                    var userToUpdate = Users.First(u => u.Id == ModalModelId);
                    userToUpdate.Email = ModalModel.Email;
                    userToUpdate.Name = ModalModel.Name;
                    userToUpdate.Phone = ModalModel.Phone;
                    userToUpdate.IsActive = ModalModel.IsActive;
                    userToUpdate.Role = ModalModel.Role;
                    await UserService.UpdateUserAsync(userToUpdate);
                    ToastService.ShowToast("Utilisateur mis à jour avec succès !", string.Empty, ToastLevel.Success);
                }
                else
                {
                    await UserService.InviteUserAsync(ModalModel.Email, ModalModel.Name, NavigationManager.BaseUri);
                    ToastService.ShowToast("Invitation envoyée avec succès !", string.Empty, ToastLevel.Success);
                }
                HideModal();
                ReloadUsers();
                await RefreshTooltipsAsync();
            }
            catch (Exception ex)
            {
                _messageStore.Add(() => ModalModel.Name, ex.Message ?? "Erreur lors de l'opération.");
                _editContext.NotifyValidationStateChanged();
                await InvokeAsync(StateHasChanged);
            }
            finally
            {
                IsBusy = false;
            }
        }


        private async Task RefreshTooltipsAsync()
        {
            await JS.InvokeVoidAsync("activateTooltips");
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

    public class UserInputModel
    {
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        public string Name { get; set; }

        public string Phone { get; set; }
        public bool IsActive { get; set; }
        public string Role { get; set; } = AuthConstants.RoleUser;
    }
}
