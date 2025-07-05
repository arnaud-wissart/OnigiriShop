using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace OnigiriShop.Pages
{
    public class AdminDashboardBase : AdminPageBase
    {
        [Inject] public IJSRuntime JS { get; set; }
        protected void GotoUsers() => Nav.NavigateTo("/admin/users");
        protected void GotoCatalog() => Nav.NavigateTo("/admin/catalog");
        protected void GotoDeliveries() => Nav.NavigateTo("/admin/deliveries");
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JS.InvokeVoidAsync("activateTooltips");
        }
    }
}