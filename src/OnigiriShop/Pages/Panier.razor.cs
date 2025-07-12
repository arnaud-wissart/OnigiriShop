using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using OnigiriShop.Data.Models;
using OnigiriShop.Services;
using System.Security.Claims;

namespace OnigiriShop.Pages
{
    [Authorize]
    public partial class Panier : ComponentBase
    {
        [Inject] public CartService CartService { get; set; }
        [Inject] public OrderService OrderService { get; set; }
        [Inject] public AuthenticationStateProvider AuthProvider { get; set; }
        [Inject] public NavigationManager Nav { get; set; }

        private bool _canAccess;
        private bool _orderSent;
        private string _resultMessage;

        private ClaimsPrincipal _user;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthProvider.GetAuthenticationStateAsync();
            _user = authState.User;
            _canAccess = _user.Identity?.IsAuthenticated == true;
        }

        private void RemoveItem(int productId)
        {
            CartService.Remove(productId);
            StateHasChanged();
        }

        private async Task SubmitOrder()
        {
            _resultMessage = null;
            if (!_canAccess)
            {
                _resultMessage = "Veuillez vous connecter.";
                return;
            }
            if (!CartService.HasItems())
            {
                _resultMessage = "Votre panier est vide.";
                return;
            }

            var userIdStr = _user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var userId))
            {
                _resultMessage = "Erreur d'identification utilisateur.";
                return;
            }

            // Livraison : à personnaliser, par défaut 1.
            int deliveryId = 1;
            decimal total = CartService.TotalPrice();

            var order = new Order
            {
                UserId = userId,
                DeliveryId = deliveryId,
                OrderedAt = DateTime.UtcNow,
                Status = "En attente",
                TotalAmount = total,
                Comment = "",
                Items = CartService.Items.Select(i => new OrderItem
                {
                    ProductId = i.Product.Id,
                    Quantity = i.Quantity,
                    UnitPrice = i.Product.Price
                }).ToList()
            };

            await OrderService.CreateOrderAsync(order, order.Items);
            _orderSent = true;
            CartService.Clear();
            _resultMessage = "Commande validée ! Merci pour votre achat.";
            StateHasChanged();
        }

        private void GoToHome()
        {
            Nav.NavigateTo("/");
        }
    }
}
