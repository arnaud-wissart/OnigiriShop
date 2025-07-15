namespace OnigiriShop.Services
{
    public class CartState
    {
        public event Action OnChanged;
        public void NotifyChanged() => OnChanged?.Invoke();
    }

}
