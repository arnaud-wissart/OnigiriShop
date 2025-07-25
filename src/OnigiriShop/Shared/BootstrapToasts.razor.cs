﻿using Microsoft.AspNetCore.Components;
using OnigiriShop.Services;

namespace OnigiriShop.Shared
{
    public class BootstrapToastsBase : ComponentBase, IDisposable
    {
        [Inject] public ToastService ToastService { get; set; } = default!;

        public List<ToastMessage> Toasts { get; set; } = new();
        private const int ToastDuration = 4000;

        protected override Task OnInitializedAsync()
        {
            ToastService.OnEnqueue += AddToast;
            return Task.CompletedTask;
        }

        private void AddToast(ToastMessage toast)
        {
            Toasts.Add(toast);
            InvokeAsync(StateHasChanged);
            _ = RemoveToastAfterDelay(toast);
        }

        private async Task RemoveToastAfterDelay(ToastMessage toast)
        {
            await Task.Delay(ToastDuration);
            RemoveToast(toast);
        }

        protected void RemoveToast(ToastMessage toast)
        {
            if (Toasts.Contains(toast))
            {
                Toasts.Remove(toast);
                InvokeAsync(StateHasChanged);
            }
        }

        protected string GetToastBg(ToastLevel level) => level switch
        {
            ToastLevel.Success => "bg-success",
            ToastLevel.Info => "bg-primary",
            ToastLevel.Warning => "bg-warning text-dark",
            ToastLevel.Error => "bg-danger",
            _ => "bg-secondary"
        };

        public void Dispose() => ToastService.OnEnqueue -= AddToast;
    }

    public class ToastMessage
    {
        public string Message { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public ToastLevel Level { get; set; }
    }
}
