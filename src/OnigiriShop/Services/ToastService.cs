using OnigiriShop.Shared;

namespace OnigiriShop.Services
{
    public class ToastService
    {
        public event Action<ToastMessage>? OnEnqueue;

        public void ShowToast(string message, string title = "", ToastLevel level = ToastLevel.Info)
            => OnEnqueue?.Invoke(new ToastMessage { Message = message, Title = title, Level = level });
    }

    public enum ToastLevel { Success, Info, Warning, Error }
}