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

    private void NavigateTo(string url)
    {
        ShowMenu = false;
        Nav.NavigateTo(url);
    }

    protected void GotoAdmin() => NavigateTo("/admin");
    protected void GotoUsers() => NavigateTo("/admin/users");
    protected void GotoCatalog() => NavigateTo("/admin/products");
    protected void GotoDeliveries() => NavigateTo("/admin/deliveries");
    protected void GotoStats() => NavigateTo("/admin/stats");
    protected void GotoEmails() => NavigateTo("/admin/emails");
    protected void GotoLogs() => NavigateTo("/admin/logs");
    protected void GotoShop() => NavigateTo("/");

    public void Dispose() => AuthProvider.AuthenticationStateChanged -= AuthStateChanged;
}
