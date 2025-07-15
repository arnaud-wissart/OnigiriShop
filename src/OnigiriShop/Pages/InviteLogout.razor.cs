using Microsoft.AspNetCore.Components;

namespace OnigiriShop.Pages
{
    public class InviteLogoutBase : ComponentBase
    {
        [Inject] public NavigationManager Nav { get; set; }

        protected override Task OnInitializedAsync()
        {
            var uri = Nav.ToAbsoluteUri(Nav.Uri);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var token = query["token"];

            Nav.NavigateTo($"/invite?token={token}", forceLoad: true);
            return Task.CompletedTask;
        }
    }
}