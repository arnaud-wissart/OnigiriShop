using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using OnigiriShop.Services;
using OnigiriShop.Data.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace OnigiriShop.Pages
{
    public class ProfileBase : ComponentBase
    {
        [Inject] public UserService UserService { get; set; }
        [Inject] public OrderService OrderService { get; set; }
        [Inject] public AuthService AuthService { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public ToastService ToastService { get; set; }

        [CascadingParameter] public Task<AuthenticationState> AuthState { get; set; }

        protected UserModel UserModel { get; set; } = new();
        protected List<Order> Orders { get; set; }
        protected Order OrderDetail { get; set; }
        protected bool EditSuccess { get; set; }
        protected string EditError { get; set; }
        protected bool IsBusy { get; set; }
        protected bool IsLoading { get; set; }
        protected bool IsSendingReset { get; set; }
        protected string ResetResult { get; set; }
        protected bool IsModified { get; set; }
        protected bool ShowResetModal { get; set; } = false;
        protected EditContext EditContext;

        private UserModel _initialUserModel = new();

        protected override async Task OnInitializedAsync()
        {
            IsLoading = true;
            try
            {
                var userIdStr = await AuthService.GetCurrentUserIdAsync();
                if (!int.TryParse(userIdStr, out var userId))
                {
                    Nav.NavigateTo("/login", true);
                    return;
                }
                var user = await UserService.GetByIdAsync(userId);
                if (user != null)
                {
                    InitModelAndEditContext(user);
                    Orders = await OrderService.GetOrdersByUserIdAsync(userId);
                }
            }
            catch (Exception ex)
            {
                EditError = "Erreur lors du chargement du profil : " + ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void HandleFieldChanged(object sender, FieldChangedEventArgs e)
        {
            IsModified = !(
                _initialUserModel.Name == UserModel.Name
                && _initialUserModel.Phone == UserModel.Phone
            );
            StateHasChanged();
        }

        protected void ShowResetConfirmModal()
        {
            ShowResetModal = true;
            StateHasChanged();
        }

        protected void HideResetConfirmModal()
        {
            ShowResetModal = false;
            StateHasChanged();
        }

        protected async Task ConfirmResetPassword()
        {
            ShowResetModal = false;
            try
            {
                await UserService.GenerateAndSendResetLinkAsync(
                    UserModel.Email,
                    UserModel.Name,
                    Nav.BaseUri
                );
                ToastService.ShowToast(
                    "Un email de réinitialisation a été envoyé. Vérifiez votre boîte de réception.",
                    "Mot de passe réinitialisé",
                    ToastLevel.Success
                );
            }
            catch (Exception ex)
            {
                ToastService.ShowToast(
                    "Erreur lors de l'envoi du mail de réinitialisation : " + ex.Message,
                    "Erreur",
                    ToastLevel.Error
                );
            }
            StateHasChanged();
        }

        protected async Task UpdateUser()
        {
            EditSuccess = false;
            EditError = null;
            IsBusy = true;

            try
            {
                await UserService.UpdateUserProfileAsync(UserModel.Id, UserModel.Name, UserModel.Phone);
                EditSuccess = true;
                var user = await UserService.GetByIdAsync(UserModel.Id);
                if (user != null)
                {
                    UserModel.Name = user.Name;
                    UserModel.Phone = user.Phone;

                    _initialUserModel.Name = user.Name;
                    _initialUserModel.Phone = user.Phone;
                }
            }
            catch (Exception ex)
            {
                EditError = ex.Message ?? "Erreur lors de la mise à jour.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected void ResetProfileForm()
        {
            // Recharge les infos user depuis la source initiale (optionnel, nécessite de garder l’état initial)
            StateHasChanged();
        }

        protected void ShowOrderDetail(Order order)
        {
            OrderDetail = order;
        }

        protected void HideOrderDetail()
        {
            OrderDetail = null;
        }
        private void InitModelAndEditContext(User user)
        {
            UserModel = new UserModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone
            };
            _initialUserModel = new UserModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone
            };
            EditContext = new EditContext(UserModel);
            EditContext.OnFieldChanged += HandleFieldChanged;
        }

        protected async Task SendPasswordReset()
        {
            ResetResult = null;
            IsSendingReset = true;
            try
            {
                var baseUrl = Nav.BaseUri;
                await UserService.GenerateAndSendResetLinkAsync(UserModel.Email, UserModel.Name, baseUrl);
                ResetResult = "Un email de réinitialisation vient d’être envoyé.";
            }
            catch (Exception ex)
            {
                ResetResult = "Erreur lors de l’envoi du lien : " + ex.Message;
            }
            finally
            {
                IsSendingReset = false;
            }
        }
    }

    public class UserModel
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required(ErrorMessage = "Le numéro de téléphone est requis")]
        [RegularExpression(@"^(0|\+33)[1-9](\d{2}){4}$", ErrorMessage = "Numéro de téléphone invalide (format FR)")]
        public string Phone { get; set; }
    }
}