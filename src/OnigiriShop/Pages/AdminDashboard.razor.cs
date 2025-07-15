using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;

namespace OnigiriShop.Pages
{
    public class AdminDashboardBase : AdminPageBase
    {
        [Inject] public OrderService OrderService { get; set; }
        [Inject] public DeliveryService DeliveryService { get; set; }
        [Inject] public IJSRuntime JS { get; set; }

        protected List<AdminOrderSummary> Orders = [];
        protected AdminOrderSummary SelectedOrder { get; set; }
        protected bool ShowOrderModal { get; set; }
        protected string FilterStatus { get; set; } = "";
        protected DateTime? FilterDeliveryDate { get; set; } = null;
        protected override async Task OnInitializedAsync() => Orders = await OrderService.GetAllAdminOrdersAsync();
        protected override async Task OnAfterRenderAsync(bool firstRender) => await JS.InvokeVoidAsync("activateTooltips");

        protected string GetStatusBadge(string status) => status switch
        {
            "En attente" => "bg-warning text-dark",
            "Validée" => "bg-success",
            "Annulée" => "bg-danger",
            _ => "bg-secondary"
        };

        protected List<AdminOrderSummary> FilteredOrders => Orders
            .Where(o =>
                (string.IsNullOrEmpty(FilterStatus) || o.Status == FilterStatus) &&
                (!FilterDeliveryDate.HasValue || o.DeliveryAt.Date == FilterDeliveryDate.Value.Date)
            )
            .ToList();

        protected List<string> StatusList => Orders.Select(o => o.Status).Distinct().ToList();

        protected void ResetFilters()
        {
            FilterStatus = "";
            FilterDeliveryDate = null;
        }

        protected void ShowOrderDetails(int orderId)
        {
            SelectedOrder = Orders.FirstOrDefault(o => o.Id == orderId);
            ShowOrderModal = true;
        }

        protected void CloseOrderModal()
        {
            ShowOrderModal = false;
            SelectedOrder = null;
        }
        protected void OnStatusChanged(ChangeEventArgs e) => FilterStatus = e.Value?.ToString();

        protected void OnDeliveryDateChanged(ChangeEventArgs e)
        {
            if (DateTime.TryParse(e.Value?.ToString(), out var dt))
                FilterDeliveryDate = dt;
            else
                FilterDeliveryDate = null;
        }
        protected string BindDate(DateTime? date) => date.HasValue ? date.Value.ToString("yyyy-MM-dd") : "";
        protected string GetFilterDate() => FilterDeliveryDate.HasValue ? FilterDeliveryDate.Value.ToString("yyyy-MM-dd") : "";
    }
}