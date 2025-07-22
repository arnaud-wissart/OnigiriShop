using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using OnigiriShop.Services;
using OnigiriShop.Data.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using OnigiriShop.Infrastructure;
using OnigiriShop.Data;

namespace OnigiriShop.Pages
{
    public class ProfileBase : FrontCustomComponentBase
    {
        [Inject] public UserService UserService { get; set; } = default!;
        [Inject] public UserAccountService UserAccountService { get; set; } = default!;
        [Inject] public OrderService OrderService { get; set; } = default!;
        [Inject] public NavigationManager Nav { get; set; } = default!;
        [Inject] public ToastService ToastService { get; set; } = default!;
        [Inject] public SessionAuthenticationStateProvider SessionAuthProvider { get; set; } = default!;
        [CascadingParameter] public Task<AuthenticationState>? AuthState { get; set; }
        protected UserModel UserModel { get; set; } = new();
        protected List<Order>? Orders { get; set; }
        protected Order? OrderDetail { get; set; }
        protected bool EditSuccess { get; set; }
        protected string? EditError { get; set; }
        protected bool IsBusy { get; set; }
        protected bool IsLoading { get; set; }
        protected bool IsSendingReset { get; set; }
        protected string? ResetResult { get; set; }
        protected bool IsModified { get; set; }
        protected bool ShowResetModal { get; set; } = false;
        protected EditContext? EditContext;

        private UserModel _initialUserModel = new();

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            IsLoading = true;
            await HandleAsync(async () =>
            {
                var userIdStr = await AuthService.GetCurrentUserIdAsync();
                if (!int.TryParse(userIdStr, out var userId))
                {
                    Nav.NavigateTo("/", true);
                    return;
                }
                var user = await UserService.GetByIdAsync(userId);
                if (user != null)
                {
                    InitModelAndEditContext(user);
                    Orders = await OrderService.GetOrdersByUserIdAsync(userId);
                }
            }, "Erreur lors du chargement du profil");
            IsLoading = false;
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
            await HandleAsync(async () =>
            {
                await UserAccountService.GenerateAndSendResetLinkAsync(
                    UserModel.Email,
                    UserModel.Name,
                    Nav.BaseUri
                );
                ToastService.ShowToast(
                    "Un email de réinitialisation a été envoyé. Vérifiez votre boîte de réception.",
                    "Mot de passe réinitialisé",
                    ToastLevel.Success
                );
            }, "Erreur lors de l'envoi du mail de réinitialisation");
            StateHasChanged();
        }

        protected async Task UpdateUser()
        {
            EditSuccess = false;
            EditError = null;
            IsBusy = true;

            await HandleAsync(async () =>
            {
                await UserService.UpdateUserProfileAsync(UserModel.Id, UserModel.Name, UserModel.Phone);
                EditSuccess = true;
                var user = await UserService.GetByIdAsync(UserModel.Id);
                if (user != null)
                {
                    UserModel.Name = user.Name!;
                    UserModel.Phone = user.Phone!;

                    _initialUserModel.Name = user.Name!;
                    _initialUserModel.Phone = user.Phone!;

                    await JS.InvokeVoidAsync("onigiriAuth.refreshSession");
                }
            }, "Erreur lors de la mise à jour");

            IsBusy = false;
        }

        protected void ResetProfileForm() => StateHasChanged();

        protected async Task ShowOrderDetailAsync(Order order)
        {
            await JS.InvokeVoidAsync("closeAllTooltips");
            OrderDetail = order;
        }

        protected void HideOrderDetail() => OrderDetail = null;
        private void InitModelAndEditContext(User user)
        {
            UserModel = new UserModel
            {
                Id = user.Id,
                Name = user.Name!,
                Email = user.Email!,
                Phone = user.Phone!
            };
            _initialUserModel = new UserModel
            {
                Id = user.Id,
                Name = user.Name!,
                Email = user.Email!,
                Phone = user.Phone!
            };
            EditContext = new EditContext(UserModel);
            EditContext.OnFieldChanged += HandleFieldChanged!;
        }

        protected async Task SendPasswordReset()
        {
            ResetResult = null;
            IsSendingReset = true;
            await HandleAsync(async () =>
            {
                var baseUrl = Nav.BaseUri;
                await UserAccountService.GenerateAndSendResetLinkAsync(UserModel.Email, UserModel.Name, baseUrl);
                ResetResult = "Un email de réinitialisation vient d’être envoyé.";
            }, "Erreur lors de l’envoi du lien");
            IsSendingReset = false;
        }
    }

    public class UserModel
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Le numéro de téléphone est requis")]
        [RegularExpression(@"^(0|\+33)[1-9](\d{2}){4}$", ErrorMessage = "Numéro de téléphone invalide (format FR)")]
        public string Phone { get; set; } = string.Empty;
    }
}
