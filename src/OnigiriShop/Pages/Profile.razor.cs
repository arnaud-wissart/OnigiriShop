using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using OnigiriShop.Services;
using OnigiriShop.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace OnigiriShop.Pages
{
    public class ProfileBase : ComponentBase
    {
        [Inject] public UserService UserService { get; set; }
        [Inject] public OrderService OrderService { get; set; }
        [Inject] public AuthService AuthService { get; set; }
        [CascadingParameter] public Task<AuthenticationState> AuthState { get; set; }

        protected UserModel UserModel { get; set; } = new();
        protected List<Order> Orders { get; set; }
        protected Order OrderDetail { get; set; }
        protected bool EditSuccess { get; set; }
        protected string EditError { get; set; }
        protected bool IsBusy { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var userIdStr = await AuthService.GetCurrentUserIdAsync();
            if (!int.TryParse(userIdStr, out var userId))
            {
                // TODO Afficher une erreur, forcer logout, etc.
                return;
            }
            var user = await UserService.GetByIdAsync(userId);
            if (user != null)
            {
                UserModel = new UserModel
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Phone = user.Phone
                };
            }

            Orders = await OrderService.GetOrdersByUserIdAsync(user.Id);
        }

        protected async Task UpdateUser()
        {
            EditSuccess = false;
            EditError = null;
            IsBusy = true;

            try
            {
                await UserService.UpdateUserProfileAsync(UserModel.Id, UserModel.Name, UserModel.Phone);
                EditSuccess = true;
            }
            catch (Exception ex)
            {
                EditError = ex.Message ?? "Erreur lors de la mise à jour.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected void ShowOrderDetail(Order order)
        {
            OrderDetail = order;
            StateHasChanged();
        }

        protected void HideOrderDetail()
        {
            OrderDetail = null;
            StateHasChanged();
        }

        
    }
    public class UserModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string Phone { get; set; }
    }
}
