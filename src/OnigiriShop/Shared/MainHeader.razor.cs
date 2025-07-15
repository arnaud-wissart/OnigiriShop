using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OnigiriShop.Services;

namespace OnigiriShop.Shared
{
    public partial class MainHeaderBase: ComponentBase
    {
        [Inject] public IJSRuntime JS { get; set; }
        [Inject] public AuthService AuthService { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Parameter] public bool IsAdminContext { get; set; }

        protected bool IsAuthenticated { get; set; }
        protected bool IsAdmin { get; set; }
        protected string UserEmail { get; set; }

        protected override async Task OnInitializedAsync()
        {
            IsAuthenticated = await AuthService.IsAuthenticatedAsync();
            IsAdmin = IsAuthenticated && await AuthService.IsAdminAsync();
            UserEmail = IsAuthenticated ? await AuthService.GetCurrentUserEmailAsync() : null;
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
    }
}