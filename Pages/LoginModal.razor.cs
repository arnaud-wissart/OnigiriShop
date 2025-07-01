using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;

namespace OnigiriShop.Pages
{
    public class LoginModalBase : ComponentBase
    {
        [Parameter] public bool Visible { get; set; }
        [Parameter] public EventCallback OnHide { get; set; }
        [Inject] public IJSRuntime JS { get; set; }

        protected LoginModel Model { get; set; } = new();
        protected string Error { get; set; }
        protected bool IsBusy { get; set; }

        protected void Hide()
        {
            Visible = false;
            OnHide.InvokeAsync();
        }

        protected async Task LoginAsync()
        {
            Error = null;
            IsBusy = true;
            try
            {
                var result = await JS.InvokeAsync<LoginResult>("onigiriAuth.login", Model.Email, Model.Password);
                if (result.Success)
                {
                    // Cookie OK, reload page to get authenticated state
                    await JS.InvokeVoidAsync("location.reload");
                }
                else
                {
                    Error = result.Error;
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        public class LoginResult
        {
            public bool Success { get; set; }
            public string Error { get; set; }
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
