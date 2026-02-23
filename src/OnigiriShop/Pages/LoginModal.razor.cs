using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace OnigiriShop.Pages
{
    public class LoginModalBase : FrontCustomComponentBase
    {
        [Parameter] public bool Visible { get; set; }
        [Parameter] public EventCallback OnHide { get; set; }

        [Inject] public SettingService SettingService { get; set; } = default!;
        [Inject] public DeliveryService DeliveryService { get; set; } = default!;

        protected LoginModel LoginModel { get; set; } = new();
        protected AccessRequestModel AccessRequestModel { get; set; } = new();
        protected ForgotPasswordModel ForgotModel { get; set; } = new();
        protected string? ErrorMessage { get; set; } = string.Empty;
        protected bool IsBusy { get; set; }
        protected bool IsRequestMode { get; set; }
        protected bool IsForgotMode { get; set; }
        protected bool RequestSent { get; set; }
        protected bool ForgotSent { get; set; }
        protected string NoAccountInfo { get; set; } = string.Empty;
        protected string RenderedNoAccountInfo { get; set; } = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            NoAccountInfo = await SettingService.GetValueAsync("NoAccountInfo") ?? string.Empty;

            var deliveries = await DeliveryService.GetUpcomingAsync(DateTime.Now, DateTime.Now.AddMonths(1));
            var places = deliveries.Select(d => d.Place).Distinct().OrderBy(p => p);
            var listMarkup = places.Any()
                ? "<ul>" + string.Join("", places.Select(p => $"<li>{WebUtility.HtmlEncode(p)}</li>")) + "</ul>"
                : string.Empty;

            RenderedNoAccountInfo = NoAccountInfo.Replace("{ListeDesLivraisons}", listMarkup);
        }

        protected void Hide()
        {
            ErrorMessage = null;
            LoginModel = new LoginModel();
            AccessRequestModel = new AccessRequestModel();
            ForgotModel = new ForgotPasswordModel();
            IsRequestMode = false;
            IsForgotMode = false;
            RequestSent = false;
            ForgotSent = false;
            OnHide.InvokeAsync();
        }

        protected async Task HandleLogin()
        {
            ErrorMessage = null;
            IsBusy = true;
            StateHasChanged();

            try
            {
                var result = await JS.InvokeAsync<LoginResult>("onigiriAuth.login", LoginModel.Email, LoginModel.Password);

                if (result != null && result.success)
                {
                    await JS.InvokeVoidAsync("location.reload");
                }
                else
                {
                    ErrorMessage = result?.error ?? "Erreur inconnue. Veuillez réessayer.";
                }
            }
            catch (JSException)
            {
                ErrorMessage = "Erreur technique côté navigateur. Rechargez la page puis réessayez.";
            }
            catch
            {
                ErrorMessage = "Erreur inattendue. Veuillez réessayer.";
            }
            finally
            {
                IsBusy = false;
                StateHasChanged();
            }
        }

        protected void ShowRequestAccess()
        {
            ErrorMessage = null;
            IsRequestMode = true;
            IsForgotMode = false;
        }

        protected void ShowLogin()
        {
            ErrorMessage = null;
            IsRequestMode = false;
            IsForgotMode = false;
        }
        protected void ShowForgotPassword()
        {
            ErrorMessage = null;
            IsForgotMode = true;
            IsRequestMode = false;
        }

        protected async Task HandleAccessRequest()
        {
            ErrorMessage = null;
            IsBusy = true;
            StateHasChanged();

            try
            {
                var result = await JS.InvokeAsync<LoginResult>("onigiriAuth.requestAccess", AccessRequestModel.Email, AccessRequestModel.Message);

                if (result != null && result.success)
                {
                    RequestSent = true;
                }
                else
                {
                    ErrorMessage = result?.error ?? "Erreur inconnue. Veuillez réessayer.";
                }
            }
            catch (JSException)
            {
                ErrorMessage = "Erreur technique côté navigateur. Rechargez la page puis réessayez.";
            }
            catch
            {
                ErrorMessage = "Erreur inattendue. Veuillez réessayer.";
            }
            finally
            {
                IsBusy = false;
                StateHasChanged();
            }
        }

        protected async Task HandleForgotPassword()
        {
            ErrorMessage = null;
            IsBusy = true;
            StateHasChanged();

            try
            {
                var result = await JS.InvokeAsync<LoginResult>("onigiriAuth.forgotPassword", ForgotModel.Email);

                if (result != null && result.success)
                {
                    ForgotSent = true;
                }
                else
                {
                    ErrorMessage = result?.error ?? "Erreur inconnue. Veuillez réessayer.";
                }
            }
            catch (JSException)
            {
                ErrorMessage = "Erreur technique côté navigateur. Rechargez la page puis réessayez.";
            }
            catch
            {
                ErrorMessage = "Erreur inattendue. Veuillez réessayer.";
            }
            finally
            {
                IsBusy = false;
                StateHasChanged();
            }
        }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Email requis")]
        [EmailAddress(ErrorMessage = "Email invalide")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Mot de passe requis")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResult
    {
        public bool success { get; set; }
        public string error { get; set; } = string.Empty;
    }

    public class AccessRequestModel
    {
        [Required(ErrorMessage = "Email requis")]
        [EmailAddress(ErrorMessage = "Email invalide")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message requis")]
        [MinLength(20, ErrorMessage = "Message trop court (20 caractères min)")]
        public string Message { get; set; } = string.Empty;
    }

    public class ForgotPasswordModel
    {
        [Required(ErrorMessage = "Email requis")]
        [EmailAddress(ErrorMessage = "Email invalide")]
        public string Email { get; set; } = string.Empty;
    }
}
