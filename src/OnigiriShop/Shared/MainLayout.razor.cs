﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using OnigiriShop.Services;
using System.Security.Claims;
using OnigiriShop.Data.Models;

namespace OnigiriShop.Shared
{
    public class MainLayoutBase : LayoutComponentBase, IDisposable
    {
        [Inject] public AuthenticationStateProvider AuthProvider { get; set; } = default!;
        [Inject] public CartService CartService { get; set; } = default!;
        [Inject] public ErrorModalService ErrorModalService { get; set; } = default!;
        [Inject] public ProductService ProductService { get; set; } = default!;
        [Inject] public NavigationManager Nav { get; set; } = default!;
        [Inject] public IJSRuntime JS { get; set; } = default!;

        protected ClaimsPrincipal? User { get; set; }
        protected string UserEmail { get; set; } = string.Empty;
        protected bool IsAdmin { get; set; }
        protected bool IsAuthenticated { get; set; }
        protected bool ShowCartSticky { get; set; } = true;

        protected List<CartItemWithProduct> CartItems { get; set; } = [];

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthProvider.GetAuthenticationStateAsync();
            User = authState.User;
            IsAuthenticated = User.Identity?.IsAuthenticated == true;
            IsAdmin = User.IsInRole("Admin");
            UserEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";

            if (IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                CartItems = await CartService.GetCartItemsWithProductsAsync(userId);
            }

            Nav.LocationChanged += OnLocationChanged!;
            ShowCartSticky = !Nav.Uri.Contains("/panier") && !Nav.Uri.Contains("/profile");
            ErrorModalService.OnShowChanged += StateHasChanged;
        }

        private void OnLocationChanged(object sender, LocationChangedEventArgs e)
        {
            ShowCartSticky = !e.Location.Contains("/panier") && !e.Location.Contains("/profile");
            StateHasChanged();
        }

        protected void GoToCart()
        {
            Nav.NavigateTo("/panier");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JS.InvokeVoidAsync("activateTooltips");
        }

        public void Dispose()
        {
            Nav.LocationChanged -= OnLocationChanged!;
            ErrorModalService.OnShowChanged -= StateHasChanged;
        }
    }
}
