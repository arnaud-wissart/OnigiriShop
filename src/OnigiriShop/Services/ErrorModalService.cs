namespace OnigiriShop.Services
{
    public class ErrorModalService
    {
        public bool Show { get; private set; }
        public string Message { get; private set; }
        public string Title { get; private set; }

        public event Action OnShowChanged;

        public void ShowModal(string message, string title = "Erreur")
        {
            Message = message;
            Title = title;
            Show = true;
            OnShowChanged?.Invoke();
        }

        public void Hide(bool val)
        {
            Show = false;
            OnShowChanged?.Invoke();
        }
    }
}
