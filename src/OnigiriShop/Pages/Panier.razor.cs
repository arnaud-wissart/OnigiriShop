using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;
using System.Security.Claims;

namespace OnigiriShop.Pages
{
    [Authorize]
    public class PanierBase : CustomComponent
    {
        [Inject] public AuthService AuthService { get; set; }
        [Inject] public EmailService EmailService { get; set; }
        [Inject] public CartService CartService { get; set; }
        [Inject] public OrderService OrderService { get; set; }
        [Inject] public DeliveryService DeliveryService { get; set; }
        [Inject] public ProductService ProductService { get; set; }
        [Inject] public AuthenticationStateProvider AuthProvider { get; set; }
        [Inject] public NavigationManager Nav { get; set; }

        protected ClaimsPrincipal _user;
        protected bool _canAccess;
        protected bool _orderSent;
        protected string _resultMessage;

        protected List<Delivery> Deliveries = [];
        protected List<Delivery> FilteredDeliveries = [];
        protected List<string> DistinctPlaces = [];

        protected int? SelectedDeliveryId = null;
        protected Delivery SelectedDelivery = null;
        protected CartItemWithProduct ItemToRemove { get; set; }
        protected bool ShowRemoveModal { get; set; }
        protected bool ShowConfirmationModal { get; set; }


        protected List<CartItemWithProduct> _items = new();
        protected decimal _totalPrice = 0;

        protected int? _userId;

        private string _selectedPlace = "";
        protected string SelectedPlace
        {
            get => _selectedPlace;
            set
            {
                if (_selectedPlace != value)
                {
                    _selectedPlace = value;
                    FilterDeliveries();
                    SelectedDeliveryId = null;
                    StateHasChanged();
                }
            }
        }
        public void ShowConfirmation()
        {
            SelectedDelivery = Deliveries.FirstOrDefault(d => d.Id == SelectedDeliveryId);
            ShowConfirmationModal = true;
        }

        public void CloseConfirmation() => ShowConfirmationModal = false;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthProvider.GetAuthenticationStateAsync();
            _user = authState.User;
            _canAccess = _user.Identity?.IsAuthenticated == true;

            var userIdStr = _user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _userId = int.TryParse(userIdStr, out var tmpId) ? tmpId : null;

            CartService.CartChanged += OnCartChanged;

            await RefreshCartAsync();

            Deliveries = await DeliveryService.GetUpcomingAsync();
            DistinctPlaces = Deliveries
                .Select(d => d.Place)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            SelectedPlace = "";
            FilterDeliveries();
        }
        private async void OnCartChanged()
        {
            await RefreshCartAsync();
            StateHasChanged();
        }
        protected async Task RefreshCartAsync()
        {
            if (_userId.HasValue)
            {
                _items = await CartService.GetCartItemsWithProductsAsync(_userId.Value) ?? [];
                _totalPrice = _items.Sum(x => x.Quantity * (x.Product?.Price ?? 0));
                StateHasChanged();
            }
        }

        protected async Task IncrementQuantity(CartItemWithProduct item)
        {
            if (_userId.HasValue)
            {
                await CartService.AddItemAsync(_userId.Value, item.ProductId, 1);
                await RefreshCartAsync();
            }
        }

        protected async Task DecrementQuantity(CartItemWithProduct item)
        {
            if (_userId.HasValue)
            {
                if (item.Quantity == 1)
                    PromptRemoveItem(item);
                else
                {
                    await CartService.RemoveItemAsync(_userId.Value, item.ProductId, 1);
                    await RefreshCartAsync();
                }
            }
        }

        protected void PromptRemoveItem(CartItemWithProduct item)
        {
            ItemToRemove = item;
            ShowRemoveModal = true;
            StateHasChanged();
        }

        protected async Task CancelRemove()
        {
            ShowRemoveModal = false;
            ItemToRemove = null;
            await RefreshCartAsync();
        }

        protected async Task ConfirmRemove()
        {
            if (_userId.HasValue && ItemToRemove != null)
                await CartService.RemoveItemAsync(_userId.Value, ItemToRemove.ProductId, ItemToRemove.Quantity);

            ShowRemoveModal = false;
            ItemToRemove = null;
            await RefreshCartAsync();
        }

        private void FilterDeliveries()
        {
            FilteredDeliveries = string.IsNullOrWhiteSpace(SelectedPlace)
                ? Deliveries.ToList()
                : Deliveries.Where(d => d.Place == SelectedPlace).ToList();
        }

        protected async Task SubmitOrder()
        {
            ShowConfirmationModal = false;
            _resultMessage = null;
            if (!_canAccess)
            {
                _resultMessage = "Veuillez vous connecter.";
                StateHasChanged();
                return;
            }
            if (_items.Count == 0)
            {
                _resultMessage = "Votre panier est vide.";
                StateHasChanged();
                return;
            }
            if (SelectedDeliveryId is null)
            {
                _resultMessage = "Veuillez sélectionner une livraison.";
                StateHasChanged();
                return;
            }

            if (!_userId.HasValue)
            {
                _resultMessage = "Erreur d'identification utilisateur.";
                StateHasChanged();
                return;
            }

            var order = new Order
            {
                UserId = _userId.Value,
                DeliveryId = SelectedDeliveryId.Value,
                OrderedAt = DateTime.UtcNow,
                Status = "En attente",
                TotalAmount = _totalPrice,
                Comment = "",
                Items = _items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.Product?.Price ?? 0
                }).ToList()
            };

            await OrderService.CreateOrderAsync(order, order.Items);
            _orderSent = true;
            await EmailService.SendOrderConfirmationAsync(
                await AuthService.GetCurrentUserEmailAsync(),
                await AuthService.GetCurrentUserNameOrEmailAsync(),
                order,
                SelectedDelivery);
            await CartService.ClearCartAsync(_userId.Value);
            _resultMessage = "Commande validée ! Merci pour votre achat.";
            StateHasChanged();
        }

        protected void GoToHome() => Nav.NavigateTo("/");
    }
}