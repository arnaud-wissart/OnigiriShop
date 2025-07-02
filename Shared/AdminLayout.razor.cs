using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using OnigiriShop.Infrastructure;

namespace OnigiriShop.Shared
{
    public class AdminLayoutBase : LayoutComponentBase, IDisposable
    {
        [Inject] public AuthenticationStateProvider AuthProvider { get; set; }
        protected bool ShowLoginModal { get; set; }
        protected bool IsAuthenticated { get; set; }
        protected bool IsAdmin { get; set; }

        protected override void OnInitialized()
        {
            AuthProvider.AuthenticationStateChanged += AuthStateChanged;
        }

        private void AuthStateChanged(Task<AuthenticationState> task)
        {
            InvokeAsync(StateHasChanged);
        }

        protected override async Task OnParametersSetAsync()
        {
            var authState = await AuthProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            IsAuthenticated = user.Identity?.IsAuthenticated == true;
            IsAdmin = user.IsInRole(AuthConstants.RoleAdmin);
            ShowLoginModal = !IsAuthenticated || !IsAdmin;
        }

        public void Dispose() => AuthProvider.AuthenticationStateChanged -= AuthStateChanged;

        protected void HideLoginModal()
        {
            ShowLoginModal = false;
            StateHasChanged();
        }
    }
}
