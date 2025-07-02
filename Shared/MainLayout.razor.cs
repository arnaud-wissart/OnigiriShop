using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using OnigiriShop.Services;
using System.Security.Claims;
using static System.Net.WebRequestMethods;

namespace OnigiriShop.Shared
{
    public partial class MainLayout : LayoutComponentBase
    {
        [Inject] public IJSRuntime JS { get; set; }
        [Inject] public NavigationManager Navigation { get; set; }
        [Inject] public HttpClient httpClient { get; set; }
        [Inject] public CartService CartService { get; set; }
        [Inject] public AuthService AuthService { get; set; }
        [CascadingParameter] public Task<AuthenticationState> AuthenticationStateTask { get; set; }
        protected ClaimsPrincipal User { get; set; }
        protected string UserEmail { get; set; }
        protected bool IsAdmin { get; set; }
        protected bool IsAuthenticated { get; set; }
        protected async Task ConfirmLogout()
        {
            // Affiche le modal Bootstrap via JSInterop
            await JS.InvokeVoidAsync("bootstrapInterop.showModal", "#logoutConfirmModal");
        }
        protected async Task OnConfirmLogout()
        {
            await JS.InvokeVoidAsync("bootstrapInterop.hideModal", "#logoutConfirmModal");
            await Logout();
        }
        protected override async Task OnInitializedAsync()
        {
            if (AuthenticationStateTask == null)
                return;
            IsAuthenticated = await AuthService.IsAuthenticatedAsync();
            UserEmail = await AuthService.GetCurrentUserEmailAsync();
            IsAdmin = await AuthService.IsAdminAsync();
        }

        protected void GoToCart()
        {
            Navigation.NavigateTo("/panier");
        }

        protected async Task Logout()
        {
            await JS.InvokeVoidAsync("onigiriAuth.logout");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JS.InvokeVoidAsync("activateTooltips"); 
        }

    }
}