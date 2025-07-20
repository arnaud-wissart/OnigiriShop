using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OnigiriShop.Services;

namespace OnigiriShop.Shared
{
    public partial class MainHeaderBase: ComponentBase
    {
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public IJSRuntime JS { get; set; }
        [Inject] public AuthService AuthService { get; set; }
        [Parameter] public bool IsAdminContext { get; set; }
        protected bool _showLoginModal;
        protected void HideLoginModal() => _showLoginModal = false;

        protected void ShowLoginModal() => _showLoginModal = true;
        protected bool IsAuthenticated { get; set; }
        protected bool IsAdmin { get; set; }
        protected string UserNameOrEmail { get; set; }

        protected override async Task OnInitializedAsync()
        {
            IsAuthenticated = await AuthService.IsAuthenticatedAsync();
            IsAdmin = IsAuthenticated && await AuthService.IsAdminAsync();
            UserNameOrEmail = IsAuthenticated ? await AuthService.GetCurrentUserNameOrEmailAsync() : null;
            StateHasChanged();
        }
        protected async Task ConfirmLogout()
        {
            await JS.InvokeVoidAsync("bootstrapInterop.showModal", "#logoutConfirmModal");
        }
        protected async Task OnConfirmLogout()
        {
            await JS.InvokeVoidAsync("bootstrapInterop.hideModal", "#logoutConfirmModal");
            await JS.InvokeVoidAsync("onigiriAuth.logout", "/");
        }
        protected void GotoAdmin() => Nav.NavigateTo("/admin");
        protected void GotoUsers() => Nav.NavigateTo("/admin/users");
        protected void GotoCatalog() => Nav.NavigateTo("/admin/products");
        protected void GotoDeliveries() => Nav.NavigateTo("/admin/deliveries");
        protected void GotoShop() => Nav.NavigateTo("/");
        protected void GotoEmails() => Nav.NavigateTo("/admin/emails");
    }
}