using Microsoft.AspNetCore.Components;
using OnigiriShop.Services;

namespace OnigiriShop.Infrastructure
{
    public class AdminPageBase : ComponentBase
    {
        [Inject] public AuthService AuthService { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
    }
}
