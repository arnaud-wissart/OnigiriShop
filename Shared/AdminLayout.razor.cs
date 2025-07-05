using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using OnigiriShop.Infrastructure;
using System.Security.Claims;
using Microsoft.JSInterop;

namespace OnigiriShop.Shared
{
    public class AdminLayoutBase : LayoutComponentBase, IDisposable
    {
        [Inject] public AuthenticationStateProvider AuthProvider { get; set; }
        [Inject] public IJSRuntime JS { get; set; }

        protected bool ShowLoginModal { get; set; }
        protected bool IsAuthenticated { get; set; }
        protected bool IsAdmin { get; set; }
        protected string UserEmail { get; set; } = "";

        protected override void OnInitialized()
        {
            AuthProvider.AuthenticationStateChanged += AuthStateChanged;
        }

        private void AuthStateChanged(Task<AuthenticationState> task)
        {
            InvokeAsync(async () =>
            {
                await UpdateAuthStateAsync();
                StateHasChanged();
            });
        }

        protected override async Task OnParametersSetAsync()
        {
            await UpdateAuthStateAsync();
        }

        private async Task UpdateAuthStateAsync()
        {
            var authState = await AuthProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            IsAuthenticated = user.Identity?.IsAuthenticated == true;
            IsAdmin = user.IsInRole(AuthConstants.RoleAdmin);
            UserEmail = user.FindFirstValue(ClaimTypes.Email) ?? "";
            ShowLoginModal = !IsAuthenticated || !IsAdmin;
        }

        public void Dispose() => AuthProvider.AuthenticationStateChanged -= AuthStateChanged;

        protected void HideLoginModal()
        {
            ShowLoginModal = false;
            StateHasChanged();
        }

        // --- Logout logic ---
        protected async Task ConfirmLogout()
        {
            await JS.InvokeVoidAsync("bootstrapInterop.showModal", "#logoutConfirmModal");
        }

        protected async Task OnConfirmLogout()
        {
            await JS.InvokeVoidAsync("bootstrapInterop.hideModal", "#logoutConfirmModal");
            await JS.InvokeVoidAsync("onigiriAuth.logout");
            await JS.InvokeVoidAsync("location.assign", "/");
            StateHasChanged();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JS.InvokeVoidAsync("activateTooltips");
        }
    }
}
