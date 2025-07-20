using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;

namespace OnigiriShop.Pages
{
    public class AdminDashboardBase : CustomComponentBase
    {
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public OrderService OrderService { get; set; }
        [Inject] public DeliveryService DeliveryService { get; set; }
        [Inject] public OrderExportService OrderExportService { get; set; }

        protected bool HasOverdueOrders { get; set; }

        protected List<AdminOrderSummary> Orders = [];
        protected AdminOrderSummary SelectedOrder { get; set; }
        protected bool ShowOrderModal { get; set; }
        protected string FilterStatus { get; set; } = "";
        protected DateTime? FilterDeliveryDate { get; set; } = DateTime.Today;
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            Orders = await OrderService.GetAllAdminOrdersAsync();
            HasOverdueOrders = Orders.Any(o => o.Status == "En attente" && o.DeliveryAt.Date < DateTime.Today);
        }
        protected void ShowOverdueOrders()
        {
            FilterStatus = "En attente";
            FilterDeliveryDate = null;
            StateHasChanged();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender) => await JS.InvokeVoidAsync("activateTooltips");
        protected AdminOrderDetail SelectedOrderDetail { get; set; }
        protected int TotalItems => FilteredOrders.Count;
        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 10;
        protected List<AdminOrderSummary> PagedOrders
            => FilteredOrders
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        protected async Task OnPageChanged(int page)
        {
            CurrentPage = page;
            await InvokeAsync(StateHasChanged);
        }
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
                (!FilterDeliveryDate.HasValue || o.DeliveryAt.Date >= FilterDeliveryDate.Value.Date)
            )
            .ToList();

        protected List<string> StatusList => Orders.Select(o => o.Status).Distinct().ToList();

        protected void ResetFilters()
        {
            FilterStatus = "";
            FilterDeliveryDate = DateTime.Today;
        }

        protected async Task ShowOrderDetails(int orderId)
        {
            SelectedOrder = Orders.FirstOrDefault(o => o.Id == orderId);

            SelectedOrderDetail = null;
            ShowOrderModal = true;

            StateHasChanged();
            SelectedOrderDetail = AdminOrderDetail.FromSummary(SelectedOrder, await OrderService.GetOrderItemsAsync(orderId));
            StateHasChanged();
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

        protected async Task ExportOrdersAsync()
        {
            if (FilteredOrders.Count == 0)
            {
                // Todo : toast ou modal pour prévenir qu'il n'y a rien à exporter
                await JS.InvokeVoidAsync("alert", "Aucune commande à exporter.");
                return;
            }

            var exportText = await OrderExportService.BuildExportTextAsync(FilteredOrders);
            var fileName = $"Commandes_{DateTime.Now:yyyyMMdd_HHmm}.txt";

            await JS.InvokeVoidAsync("downloadFileFromText", fileName, exportText);
        }

        protected string BindDate(DateTime? date) => date.HasValue ? date.Value.ToString("yyyy-MM-dd") : "";
    }
}