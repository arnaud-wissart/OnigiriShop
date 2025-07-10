using Microsoft.AspNetCore.Components;

namespace OnigiriShop.Pages
{
    public class InviteLogoutBase : ComponentBase
    {
        [Inject] public NavigationManager Nav { get; set; }

        protected override void OnInitialized()
        {
            var uri = Nav.ToAbsoluteUri(Nav.Uri);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var token = query["token"];

            // Petite pause optionnelle (sinon ça va si vite qu’on ne voit rien…)
            // await Task.Delay(300); // Tu peux activer en passant en async si tu veux

            Nav.NavigateTo($"/invite?token={token}", forceLoad: true);
        }
    }
}
