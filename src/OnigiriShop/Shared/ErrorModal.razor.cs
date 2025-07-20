using Microsoft.AspNetCore.Components;

namespace OnigiriShop.Shared
{
    public class ErrorModalBase : ComponentBase
    {
        [Parameter] public bool Show { get; set; }
        [Parameter] public EventCallback<bool> ShowChanged { get; set; }
        [Parameter] public string Message { get; set; } = string.Empty;
        [Parameter] public string Title { get; set; } = "Erreur";

        protected async Task Close()
        {
            if (ShowChanged.HasDelegate)
                await ShowChanged.InvokeAsync(false);
        }
    }
}