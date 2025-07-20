using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OnigiriShop.Services;

namespace OnigiriShop.Infrastructure
{
    public abstract class CustomComponentBase : ComponentBase
    {
        [Inject] public IJSRuntime JS { get; set; } = default!;

        [Inject] public ErrorModalService ErrorModalService { get; set; } = default!;
        [Inject] public AuthService AuthService { get; set; } = default!;
        protected Task HandleAsync(Func<Task> action, string userMessage = null, string title = null, bool showExceptionMessage = false)
            => BlazorExceptionHandler.HandleAsync(action, ErrorModalService, userMessage, title, showExceptionMessage);

        protected bool UserIsConnected;
        protected int UserId { get; set; } = -1;

        protected override async Task OnInitializedAsync()
        {
            UserIsConnected = await AuthService.IsAuthenticatedAsync();
            if (UserIsConnected)
                UserId = (await AuthService.GetCurrentUserIdIntAsync()).Value;
        }
    }

    public abstract class FrontCustomComponentBase : CustomComponentBase
    {
        [Inject] public CartService CartService { get; set; } = default!;
        [Inject] public AnonymousCartService AnonymousCartService { get; set; } = default!;
        [Inject] public CartState CartState { get; set; } = default!;
        [Inject] public CartProvider CartProvider { get; set; } = default!;
    }
}