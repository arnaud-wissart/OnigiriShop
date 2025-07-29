using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using OnigiriShop.Infrastructure;

namespace OnigiriShop.Shared;

public class AdminFabMenuBase : CustomComponentBase, IDisposable
{
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthProvider { get; set; } = default!;

    protected bool IsAdmin { get; set; }
    protected bool ShowMenu { get; set; }

    protected bool IsAdminPage => Nav.ToBaseRelativePath(Nav.Uri).StartsWith("admin", StringComparison.OrdinalIgnoreCase);

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        AuthProvider.AuthenticationStateChanged += AuthStateChanged;
        IsAdmin = await AuthService.IsAdminAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender) => await JS.InvokeVoidAsync("activateTooltips");

    private async void AuthStateChanged(Task<AuthenticationState> task)
    {
        IsAdmin = await AuthService.IsAdminAsync();
        await InvokeAsync(StateHasChanged);
    }

    protected async Task ToggleMenu()
    {
        ShowMenu = !ShowMenu;
        await JS.InvokeVoidAsync("activateTooltips");
    }

    protected void GotoAdmin() => Nav.NavigateTo("/admin");
    protected void GotoUsers() => Nav.NavigateTo("/admin/users");
    protected void GotoCatalog() => Nav.NavigateTo("/admin/products");
    protected void GotoDeliveries() => Nav.NavigateTo("/admin/deliveries");
    protected void GotoStats() => Nav.NavigateTo("/admin/stats");
    protected void GotoEmails() => Nav.NavigateTo("/admin/emails");
    protected void GotoLogs() => Nav.NavigateTo("/admin/logs");
    protected void GotoShop() => Nav.NavigateTo("/");

    public void Dispose() => AuthProvider.AuthenticationStateChanged -= AuthStateChanged;
}
