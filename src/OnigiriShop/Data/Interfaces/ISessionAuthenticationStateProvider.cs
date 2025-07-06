using OnigiriShop.Data.Models;

namespace OnigiriShop.Data.Interfaces
{
    public interface ISessionAuthenticationStateProvider
    {
        Task SignOutAsync();
        Task SignInAsync(User user);
    }
}