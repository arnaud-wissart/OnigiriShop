using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using OnigiriShop.Services;

namespace OnigiriShop.Shared
{
    public partial class MainHeaderBase: ComponentBase
    {
        [Inject] public AuthenticationStateProvider AuthProvider { get; set; } = default!;
        [Inject] public NavigationManager Nav { get; set; } = default!;
        [Inject] public IJSRuntime JS { get; set; } = default!;
        [Inject] public AuthService AuthService { get; set; } = default!;
        [Inject] public SiteNameService SiteNameService { get; set; } = default!;
        [Parameter] public bool IsAdminContext { get; set; }
        protected bool _showLoginModal;
        protected void HideLoginModal() => _showLoginModal = false;

        protected void ShowLoginModal() => _showLoginModal = true;
        protected bool IsAuthenticated { get; set; }
        protected bool IsAdmin { get; set; }
        protected string? UserNameOrEmail { get; set; }
        protected string SiteName { get; set; } = string.Empty;
        protected bool IsEditing { get; set; }
        protected string EditedSiteName { get; set; } = string.Empty;
        protected bool ShowConfirmModal { get; set; }

        protected override async Task OnInitializedAsync()
        {
            AuthProvider.AuthenticationStateChanged += AuthStateChanged;
            await UpdateAuthInfoAsync();
            SiteName = await SiteNameService.GetSiteNameAsync();
        }

        private async void AuthStateChanged(Task<AuthenticationState> task)
        {
            await UpdateAuthInfoAsync();
            StateHasChanged();
        }

        private async Task UpdateAuthInfoAsync()
        {
            IsAuthenticated = await AuthService.IsAuthenticatedAsync();
            IsAdmin = IsAuthenticated && await AuthService.IsAdminAsync();
            UserNameOrEmail = IsAuthenticated ? await AuthService.GetCurrentUserNameOrEmailAsync() : null;
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
        protected void GotoStats() => Nav.NavigateTo("/admin/stats");
        protected void GotoShop() => Nav.NavigateTo("/");
        protected void GotoEmails() => Nav.NavigateTo("/admin/emails");
        protected void GotoLogs() => Nav.NavigateTo("/admin/logs");
        protected void StartEdit()
        {
            EditedSiteName = SiteName;
            IsEditing = true;
        }

        protected void CancelEdit() => IsEditing = false;

        protected void RequestSave() => ShowConfirmModal = true;

        protected void CancelConfirm() => ShowConfirmModal = false;

        protected async Task SaveAsync()
        {
            await SiteNameService.SetSiteNameAsync(EditedSiteName.Trim());
            SiteName = EditedSiteName.Trim();
            ShowConfirmModal = false;
            IsEditing = false;
        }
    }
}
