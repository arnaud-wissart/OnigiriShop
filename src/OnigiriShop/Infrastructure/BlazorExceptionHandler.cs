using OnigiriShop.Services;

namespace OnigiriShop.Infrastructure
{
    public static class BlazorExceptionHandler
    {
        public static async Task HandleAsync(
            Func<Task> action,
            ErrorModalService errorService,
            string? userMessage = null,
            string? title = null,
            bool showExceptionMessage = false
        )
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erreur non gérée dans un composant Blazor");
                var displayMessage = userMessage ?? "Une erreur inattendue s'est produite. Veuillez réessayer.";

                if (showExceptionMessage)
                {
                    displayMessage += "<br /><small class='text-muted'>Détail : " + ex.Message + "</small>";
                }

                errorService.ShowModal(displayMessage, title ?? "Erreur");
            }
        }
    }
}
