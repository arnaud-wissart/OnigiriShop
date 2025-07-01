using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using OnigiriShop.Services;
using System.Security.Claims;

namespace OnigiriShop.Shared
{
    public partial class MainLayout : LayoutComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; }
        [Inject] public CartService CartService { get; set; }

        [CascadingParameter] public Task<AuthenticationState> AuthenticationStateTask { get; set; }

        protected ClaimsPrincipal User { get; set; }
        public bool IsAuthenticated { get; private set; }
        public string UserName { get; private set; }

        protected override async Task OnInitializedAsync()
        {
            if (AuthenticationStateTask == null)
            {
                IsAuthenticated = false;
                UserName = null;
                return;
            }
            var authState = await AuthenticationStateTask;
            var user = authState.User;
            IsAuthenticated = user?.Identity?.IsAuthenticated == true;
            UserName = user?.Identity?.Name;
        }

        protected void GoToCart()
        {
            Navigation.NavigateTo("/panier");
        }
    }
}