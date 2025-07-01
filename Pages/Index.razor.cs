using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using OnigiriShop.Data;
using OnigiriShop.Services;

namespace OnigiriShop.Pages
{
    public class IndexBase : ComponentBase, IDisposable
    {
        [Inject] public CartService CartService { get; set; }
        [Inject] public IServiceProvider ServiceProvider { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public ActiveCatalogManager ActiveCatalogManager { get; set; }
        [Inject] public ProductService ProductService { get; set; }
        [CascadingParameter] public Task<AuthenticationState> AuthenticationStateTask { get; set; }

        protected List<Product> _products;
        protected HashSet<int> _addedProductIds = [];
        protected bool _showLoginModal;
        protected void ShowLoginModal() => _showLoginModal = true;
        protected void HideLoginModal() => _showLoginModal = false;

        protected override async Task OnInitializedAsync()
        {
            var state = await AuthenticationStateTask;
            var user = state.User;
            var isAuth = user.Identity?.IsAuthenticated;
            var activeCatalog = await ActiveCatalogManager.GetActiveCatalogAsync();
            _products = await ProductService.GetAllAsync();
            CartService.CartChanged += OnCartChanged;
        }
        private void OnCartChanged() => InvokeAsync(StateHasChanged);
        public void Dispose() => CartService.CartChanged -= OnCartChanged;
        protected string GetCartIconClass(int qty) => qty > 0 ? "bi bi-cart-check-fill" : "bi bi-cart-plus";
        public async Task TryAddToCart(Product product)
        {
            var authState = await AuthenticationStateTask;
            var user = authState.User;

            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                ShowLoginModal();
                return;
            }
            CartService.Add(product);
            _addedProductIds.Add(product.Id);
            StateHasChanged();
            // Attendre 200ms puis retirer la classe effet
            await Task.Delay(200);
            _addedProductIds.Remove(product.Id);
            StateHasChanged();
        }
        public async Task AddToCart(Product product)
        {
            var authState = await AuthenticationStateTask;
            if (authState.User.Identity?.IsAuthenticated != true)
            {
                ShowLoginModal();
                return;
            }
            CartService.Add(product, 1);
            StateHasChanged();
        }
        public async Task RemoveFromCart(Product product)
        {
            var authState = await AuthenticationStateTask;
            if (authState.User.Identity?.IsAuthenticated != true)
            {
                ShowLoginModal();
                return;
            }
            if (CartService.GetProductCount(product.Id) > 0)
            {
                CartService.Remove(product.Id, 1);
                StateHasChanged();
            }
        }
    }
}