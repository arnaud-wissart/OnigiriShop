using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using OnigiriShop.Data.Models;
using OnigiriShop.Services;
using System.Security.Claims;
using static OnigiriShop.Services.CartService;

namespace OnigiriShop.Pages
{
    [Authorize]
    public class PanierBase : ComponentBase, IDisposable
    {
        [CascadingParameter] public Action<bool> SetCartStickyVisible { get; set; }
        [Inject] public CartService CartService { get; set; }
        [Inject] public OrderService OrderService { get; set; }
        [Inject] public DeliveryService DeliveryService { get; set; }
        [Inject] public ProductService ProductService { get; set; }
        [Inject] public AuthenticationStateProvider AuthProvider { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public ToastService ToastService { get; set; }
        [Inject] IJSRuntime JS { get; set; }

        protected ClaimsPrincipal _user;
        protected bool _canAccess;
        protected bool _orderSent;
        protected string _resultMessage;

        protected List<Delivery> Deliveries = [];
        protected List<Delivery> FilteredDeliveries = [];
        protected List<string> DistinctPlaces = [];

        // Produits en mémoire pour résolution rapide
        private List<Product> _allProducts = [];

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
                    SelectedDeliveryId = FilteredDeliveries.Count > 0
                        ? FilteredDeliveries.First().Id
                        : null;
                    StateHasChanged();
                }
            }
        }

        protected int? SelectedDeliveryId;
        protected CartItem ItemToRemove { get; set; }
        protected bool ShowRemoveModal { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthProvider.GetAuthenticationStateAsync();
            _user = authState.User;
            _canAccess = _user.Identity?.IsAuthenticated == true;

            // 1. Charger tous les produits (tu peux faire GetAllAsync ou GetMenuProductsAsync selon ton besoin)
            _allProducts = await ProductService.GetAllAsync();

            // 2. Charger le panier depuis le localStorage JS
            await CartService.LoadFromLocalStorageAsync(ResolveProductById);

            // 3. Charger les livraisons à venir
            Deliveries = await DeliveryService.GetUpcomingAsync();
            DistinctPlaces = Deliveries
                .Select(d => d.Place)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            SelectedPlace = "";
            FilterDeliveries();

            if (FilteredDeliveries.Count > 0)
                SelectedDeliveryId = FilteredDeliveries.First().Id;

            SetCartStickyVisible?.Invoke(false);
        }

        // Méthode de résolution rapide du produit par Id (pas d’appel DB !)
        private Product ResolveProductById(int id)
            => _allProducts.FirstOrDefault(p => p.Id == id);

        protected void IncrementQuantity(CartItem item)
        {
            CartService.Add(item.Product, 1);
            StateHasChanged();
        }

        protected void DecrementQuantity(CartItem item)
        {
            if (item.Quantity == 1)
                PromptRemoveItem(item);
            else
            {
                CartService.Remove(item.Product.Id, 1);
                StateHasChanged();
            }
        }

        protected void PromptRemoveItem(CartItem item)
        {
            ItemToRemove = item;
            ShowRemoveModal = true;
            StateHasChanged();
        }

        protected void CancelRemove()
        {
            ShowRemoveModal = false;
            ItemToRemove = null;
        }

        protected void ConfirmRemove()
        {
            if (ItemToRemove != null)
            {
                CartService.Remove(ItemToRemove.Product.Id, ItemToRemove.Quantity);
                ToastService.ShowToast(
                    $"Produit retiré du panier : {ItemToRemove.Product.Name}", "Panier", ToastLevel.Info);
            }
            ShowRemoveModal = false;
            ItemToRemove = null;
            StateHasChanged();
        }

        protected void OnDeliveryChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var val))
                SelectedDeliveryId = val;
            else
                SelectedDeliveryId = null;
        }

        private void FilterDeliveries()
        {
            FilteredDeliveries = string.IsNullOrWhiteSpace(SelectedPlace)
                ? Deliveries.ToList()
                : Deliveries.Where(d => d.Place == SelectedPlace).ToList();
        }

        protected async Task SubmitOrder()
        {
            _resultMessage = null;
            if (!_canAccess)
            {
                _resultMessage = "Veuillez vous connecter.";
                StateHasChanged();
                return;
            }
            if (!CartService.HasItems())
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

            var userIdStr = _user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var userId))
            {
                _resultMessage = "Erreur d'identification utilisateur.";
                StateHasChanged();
                return;
            }

            var order = new Order
            {
                UserId = userId,
                DeliveryId = SelectedDeliveryId.Value,
                OrderedAt = DateTime.UtcNow,
                Status = "En attente",
                TotalAmount = CartService.TotalPrice(),
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
            ToastService.ShowToast("Commande validée !", "Panier", ToastLevel.Success);
            StateHasChanged();
        }

        protected void GoToHome() => Nav.NavigateTo("/");

        public void Dispose() => SetCartStickyVisible?.Invoke(true);
    }
}
