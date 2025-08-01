using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;

namespace OnigiriShop.Pages
{
    public class PanierBase : FrontCustomComponentBase, IDisposable
    {
        [Inject] public EmailService EmailService { get; set; } = default!;
        [Inject] public OrderService OrderService { get; set; } = default!;
        [Inject] public DeliveryService DeliveryService { get; set; } = default!;
        [Inject] public IProductService ProductService { get; set; } = default!;
        [Inject] public AuthenticationStateProvider AuthProvider { get; set; } = default!;
        [Inject] public NavigationManager Nav { get; set; } = default!;

        protected DotNetObjectReference<PanierBase>? objRef;

        protected bool _orderSent;
        protected string? _resultMessage;

        protected List<Delivery> Deliveries = [];
        protected List<Delivery> FilteredDeliveries = [];
        protected List<string> DistinctPlaces = [];

        protected int? SelectedDeliveryId = null;
        protected Delivery? SelectedDelivery;
        protected CartItemWithProduct? ItemToRemove { get; set; }
        protected bool ShowRemoveModal { get; set; }
        protected bool ShowConfirmationModal { get; set; }
        protected List<CartItemWithProduct> _items = [];

        protected decimal _totalPrice = 0;

        private string _selectedPlace = "";
        private const string SelectedPlaceKey = "selectedPlace";
        private const string SelectedDeliveryKey = "selectedDelivery";
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
                    _ = JS.InvokeVoidAsync("localStorage.setItem", SelectedPlaceKey, value);
                    _ = JS.InvokeVoidAsync("localStorage.removeItem", SelectedDeliveryKey);
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
            await base.OnInitializedAsync();

            objRef = DotNetObjectReference.Create(this);

            CartState.OnChanged += OnCartChanged;

            _items = await CartProvider.GetCurrentCartItemsWithProductsAsync();
            _totalPrice = _items.Sum(x => x.Quantity * (x.Product?.Price ?? 0));

            Deliveries = await DeliveryService.GetUpcomingAsync(DateTime.Now, DateTime.Now.AddMonths(1));
            DistinctPlaces = Deliveries
                .Select(d => d.Place)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            var savedPlace = await JS.InvokeAsync<string>("localStorage.getItem", SelectedPlaceKey);
            if (!string.IsNullOrWhiteSpace(savedPlace) && DistinctPlaces.Contains(savedPlace))
                SelectedPlace = savedPlace;
            else
                SelectedPlace = string.Empty;

            FilterDeliveries();

            var savedDelivery = await JS.InvokeAsync<string>("localStorage.getItem", SelectedDeliveryKey);
            if (int.TryParse(savedDelivery, out var delId) && Deliveries.Any(d => d.Id == delId))
            {
                SelectedDeliveryId = delId;
                SelectedDelivery = Deliveries.FirstOrDefault(d => d.Id == delId);
            }
        }
        private void OnCartChanged() => InvokeAsync(RefreshCartAsync);
        protected async Task RefreshCartAsync()
        {
            _items = await CartProvider.GetCurrentCartItemsWithProductsAsync();
            _totalPrice = _items.Sum(x => x.Quantity * (x.Product?.Price ?? 0));
            StateHasChanged();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
                await RefreshDatePickerAsync();
        }

        protected async Task IncrementQuantity(CartItemWithProduct item)
        {
            await CartProvider.AddItemAsync(item.ProductId, 1);
            CartState.NotifyChanged();
        }
        protected async Task DecrementQuantity(CartItemWithProduct item)
        {
            if (item.Quantity == 1)
                PromptRemoveItem(item);
            else
            {
                await CartProvider.RemoveItemAsync(item.ProductId, 1);
                CartState.NotifyChanged();
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
            if (ItemToRemove != null)
                await CartProvider.RemoveItemAsync(ItemToRemove.ProductId, ItemToRemove.Quantity);

            ShowRemoveModal = false;
            ItemToRemove = null;
            CartState.NotifyChanged();
        }

        private void FilterDeliveries()
        {
            FilteredDeliveries = string.IsNullOrWhiteSpace(SelectedPlace)
                ? Deliveries.ToList()
                : Deliveries.Where(d => d.Place == SelectedPlace).ToList();

            _ = InvokeAsync(RefreshDatePickerAsync);
        }

        private Task RefreshDatePickerAsync()
        {
            var list = FilteredDeliveries.Select(d => new
            {
                id = d.Id,
                date = d.DeliveryAt.ToString("yyyy-MM-dd HH:mm")
            }).ToList();
            return JS.InvokeVoidAsync("onigiriDatePicker.init", "deliveryDateInput", objRef!, list).AsTask();
        }

        [JSInvokable]
        public void OnDateSelected(int deliveryId)
        {
            SelectedDeliveryId = deliveryId;
            SelectedDelivery = Deliveries.FirstOrDefault(d => d.Id == deliveryId);
            _ = JS.InvokeVoidAsync("localStorage.setItem", SelectedDeliveryKey, deliveryId.ToString());
            StateHasChanged();
        }

        protected async Task SubmitOrder()
        {
            ShowConfirmationModal = false;
            _resultMessage = null;
            if (!UserIsConnected)
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
            if (string.IsNullOrWhiteSpace(SelectedPlace))
            {
                _resultMessage = "Veuillez sélectionner un lieu de livraison.";
                StateHasChanged();
                return;
            }
            if (SelectedDeliveryId is null)
            {
                _resultMessage = "Veuillez sélectionner une livraison.";
                StateHasChanged();
                return;
            }

            var order = new Order
            {
                UserId = UserId,
                DeliveryId = SelectedDeliveryId.Value,
                OrderedAt = DateTime.UtcNow,
                Status = "En attente",
                TotalAmount = _totalPrice,
                Comment = "",
                Items = _items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.Product?.Price ?? 0,
                    ProductName = i.Product?.Name ?? string.Empty
                }).ToList()
            };

            var orderId = await OrderService.CreateOrderAsync(order, order.Items);
            order.Id = orderId;
            _orderSent = true;
            await EmailService.SendOrderConfirmationAsync(
                await AuthService.GetCurrentUserEmailAsync(),
                await AuthService.GetCurrentUserNameOrEmailAsync(),
                order,
                SelectedDelivery!);

            var upcoming = (await OrderService.GetAllAdminOrdersAsync())
                .Where(o => o.Status == "En attente" && o.DeliveryAt <= DateTime.Now.AddDays(7))
                .OrderBy(o => o.DeliveryAt)
                .ToList();

            var lines = string.Join("", upcoming.Select(o => $"<li>#{o.Id} - {o.UserDisplayName} - {o.DeliveryAt:dd/MM/yyyy HH:mm}</li>"));
            var htmlAdmin = $"<p>Nouvelle commande #{order.Id} pour le {SelectedDelivery!.DeliveryAt:dd/MM/yyyy HH:mm}.</p><p>Commandes à venir :</p><ul>{lines}</ul>";
            var textAdmin = $"Nouvelle commande #{order.Id} pour le {SelectedDelivery!.DeliveryAt:dd/MM/yyyy HH:mm}.\n" +
                             string.Join("\n", upcoming.Select(o => $"#{o.Id} - {o.UserDisplayName} - {o.DeliveryAt:dd/MM/yyyy HH:mm}"));
            await EmailService.SendAdminNotificationAsync("Nouvelle commande", htmlAdmin, textAdmin);

            await CartProvider.ClearCartAsync();
            await CartProvider.RefreshCartStateAsync(CartState);
            CartState.NotifyChanged();
            _items.Clear();
            _totalPrice = 0;
            SelectedDeliveryId = null;
            SelectedDelivery = null;
            SelectedPlace = "";
            await JS.InvokeVoidAsync("localStorage.removeItem", SelectedPlaceKey);
            await JS.InvokeVoidAsync("localStorage.removeItem", SelectedDeliveryKey);
            _resultMessage = "Commande validée ! Merci pour votre achat.";
            StateHasChanged();
        }

        public void Dispose()
        {
            CartState.OnChanged -= OnCartChanged;
            objRef?.Dispose();
        }

    }
}
