using Microsoft.AspNetCore.Components;
using OnigiriShop.Services;

namespace OnigiriShop.Pages
{
    public partial class CartSummary : ComponentBase
    {
        [Inject] public CartService CartService { get; set; }
        [Inject] public IServiceProvider ServiceProvider { get; set; }
        [Inject] public NavigationManager Nav { get; set; }

        private bool _canOrder;

        protected override async Task OnInitializedAsync()
        {
            //_canOrder = await AuthService.IsUserWhitelistedAsync(ServiceProvider);
            _canOrder = false;
        }

        private void GotoPanier()
        {
            Nav.NavigateTo("/panier");
        }

        private void GotoLogin()
        {
            Nav.NavigateTo("/login");
        }
    }
}
