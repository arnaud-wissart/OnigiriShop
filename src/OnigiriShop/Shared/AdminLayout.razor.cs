using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using OnigiriShop.Infrastructure;
using System.Security.Claims;
using Microsoft.JSInterop;
using OnigiriShop.Services;

namespace OnigiriShop.Shared
{
    public class AdminLayoutBase : LayoutComponentBase, IDisposable
    {
        [Inject] public ErrorModalService ErrorModalService { get; set; } = default!;

        [Inject] public AuthenticationStateProvider AuthProvider { get; set; } = default!;
        [Inject] public IJSRuntime JS { get; set; } = default!;

        protected bool ShowLoginModal { get; set; }
        protected bool IsAuthenticated { get; set; }
        protected bool IsAdmin { get; set; }
        protected string UserEmail { get; set; } = "";

        protected override Task OnInitializedAsync()
        {
            AuthProvider.AuthenticationStateChanged += AuthStateChanged;
            ErrorModalService.OnShowChanged += StateHasChanged;
            return Task.CompletedTask;
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

        public void Dispose()
        {
            AuthProvider.AuthenticationStateChanged -= AuthStateChanged;
            ErrorModalService.OnShowChanged -= StateHasChanged;
        }

        protected void HideLoginModal()
        {
            ShowLoginModal = false;
            StateHasChanged();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JS.InvokeVoidAsync("activateTooltips");
        }
    }
}
