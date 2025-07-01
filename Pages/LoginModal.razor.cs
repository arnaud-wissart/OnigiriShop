using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;
using OnigiriShop.Data;
using OnigiriShop.Services;
using System.ComponentModel.DataAnnotations;

namespace OnigiriShop.Pages
{
    public class LoginModalBase : ComponentBase
    {
        [Parameter] public bool Visible { get; set; }
        [Parameter] public EventCallback OnHide { get; set; }
        [Inject] public HttpClient Http { get; set; }
        [Inject] public IJSRuntime JS { get; set; }
        [Inject] public NavigationManager Nav { get; set; }

        protected LoginModel Model { get; set; } = new();
        protected string Error { get; set; }
        protected bool IsBusy { get; set; }

        protected void Hide()
        {
            Visible = false;
            OnHide.InvokeAsync();
        }

        public class LoginModel
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        protected async Task LoginAsync()
        {
            Error = null;
            IsBusy = true;
            try
            {
                var response = await Http.PostAsJsonAsync("/api/auth/login", Model);
                if (response.IsSuccessStatusCode)
                {
                    await JS.InvokeVoidAsync("console.log", "Attente avant reload…");
                    await Task.Delay(5000); // 5 secondes pour inspecter le cookie dans l’onglet Application de DevTools
                    await JS.InvokeVoidAsync("location.reload");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Error = "Email ou mot de passe incorrect.";
                }
                else
                {
                    Error = $"Erreur {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Email obligatoire")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mot de passe obligatoire")]
        public string Password { get; set; }
    }

    public class ForgotModel
    {
        [Required(ErrorMessage = "Email obligatoire")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; }
    }
}
