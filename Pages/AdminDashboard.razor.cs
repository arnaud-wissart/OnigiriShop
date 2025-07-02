using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace OnigiriShop.Pages
{
    public class AdminDashboardBase : AdminPageBase
    {
        [Inject] public IJSRuntime JS { get; set; }

        protected void GotoUsers()
        {
            Nav.NavigateTo("/admin/users");
        }

        protected void GotoCatalog()
        {
            Nav.NavigateTo("/admin/catalog");
        }
        protected override async Task OnInitializedAsync()
        {
            await JS.InvokeVoidAsync("hideAllTooltips");
        }
    }
}
