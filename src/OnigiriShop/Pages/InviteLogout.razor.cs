using Microsoft.AspNetCore.Components;
using OnigiriShop.Infrastructure;

namespace OnigiriShop.Pages
{
    public class InviteLogoutBase : CustomComponentBase
    {
        [Inject] public NavigationManager Nav { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            var uri = Nav.ToAbsoluteUri(Nav.Uri);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var token = query["token"];

            Nav.NavigateTo($"/invite?token={token}", forceLoad: true);
        }
    }
}
