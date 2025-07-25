﻿using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OnigiriShop.Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace OnigiriShop.Pages
{
    public class LoginModalBase : FrontCustomComponentBase
    {
        [Parameter] public bool Visible { get; set; }
        [Parameter] public EventCallback OnHide { get; set; }

        protected LoginModel LoginModel { get; set; } = new();
        protected string? ErrorMessage { get; set; } = string.Empty;
        protected bool IsBusy { get; set; }

        protected void Hide()
        {
            ErrorMessage = null;
            LoginModel = new LoginModel();
            OnHide.InvokeAsync();
        }

        protected async Task HandleLogin()
        {
            ErrorMessage = null;
            IsBusy = true;
            StateHasChanged();

            var result = await JS.InvokeAsync<LoginResult>("onigiriAuth.login", LoginModel.Email, LoginModel.Password);

            if (result != null && result.success)
            {
                await JS.InvokeVoidAsync("location.reload");
            }
            else
            {
                ErrorMessage = result?.error ?? "Erreur inconnue. Veuillez réessayer.";
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
}
