using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using OnigiriShop.Services;
using System.Security.Claims;

namespace OnigiriShop.Shared
{
    public class MainLayoutBase : LayoutComponentBase
    {
        [Inject] public AuthenticationStateProvider AuthProvider { get; set; }
        [Inject] public CartService CartService { get; set; }
        [Inject] public ProductService ProductService { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public IJSRuntime JS { get; set; }

        protected ClaimsPrincipal User { get; set; }
        protected string UserEmail { get; set; }
        protected bool IsAdmin { get; set; }
        protected bool IsAuthenticated { get; set; }
        protected bool ShowCartSticky { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthProvider.GetAuthenticationStateAsync();
            User = authState.User;
            IsAuthenticated = User.Identity?.IsAuthenticated == true;
            IsAdmin = User.IsInRole("Admin");
            UserEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";
            var products = await ProductService.GetAllAsync();
            await CartService.InitializeAsync(id => products.FirstOrDefault(p => p.Id == id));
        }

        protected void GoToCart()
        {
            Nav.NavigateTo("/panier");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            ShowCartSticky = !Nav.Uri.Contains("/panier");
            StateHasChanged();
            await JS.InvokeVoidAsync("activateTooltips");
        }

    }
}
