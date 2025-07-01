using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using OnigiriShop.Data;
using OnigiriShop.Services;
using System.Security.Claims;

namespace OnigiriShop.Pages
{
    public partial class Panier : ComponentBase
    {
        [Inject] public CartService CartService { get; set; }
        [Inject] public OrderManager OrderManager { get; set; }
        [Inject] public IServiceProvider ServiceProvider { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [CascadingParameter] public Task<AuthenticationState> AuthenticationStateTask { get; set; }

        private bool _canAccess;
        private bool _orderSent;
        private string _resultMessage;

        private ClaimsPrincipal _user;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateTask;
            _user = authState.User;

            _canAccess = _user.Identity?.IsAuthenticated == true;
            if (!_canAccess)
            {
                Nav.NavigateTo("/login");
            }
        }

        private void RemoveItem(int productId)
        {
            CartService.Remove(productId);
            StateHasChanged();
        }

        private async Task SubmitOrder()
        {
            // Sécurité : on vérifie encore l’état utilisateur avant commande
            if (_user == null || !_user.Identity?.IsAuthenticated == true)
            {
                _resultMessage = "Veuillez vous connecter.";
                return;
            }
            if (CartService.TotalCount() == 0)
            {
                _resultMessage = "Votre panier est vide.";
                return;
            }
            var userId = _user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = _user.Identity?.Name;
            var email = _user.FindFirst(ClaimTypes.Email)?.Value;

            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                UserId = userId,
                UserDisplayName = username,
                UserEmail = email,
                OrderDate = DateTime.UtcNow,
                Items = CartService.Items.Select(i => new OrderItem
                {
                    ProductName = i.Product.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.Product.Price
                }).ToList()
            };
            await OrderManager.AddOrderAsync(order);
            _orderSent = true;
            CartService.Clear();
            _resultMessage = "Commande validée ! Merci pour votre achat.";
            StateHasChanged();
        }
    }
}
