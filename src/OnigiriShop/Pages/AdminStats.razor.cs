using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;

namespace OnigiriShop.Pages;

public class AdminStatsBase : CustomComponentBase
{
    [Inject] public StatsService StatsService { get; set; } = default!;
    protected int OrdersYear { get; set; }
    protected int RevenueYear { get; set; }
    protected DateTime ProductsStart { get; set; }
    protected DateTime ProductsEnd { get; set; }
    protected int[] Orders { get; set; } = new int[12];
    protected decimal[] Revenue { get; set; } = new decimal[12];
    protected ProductStatsResult? ProductResult { get; set; }
    private bool _updateOrdersChart;
    private bool _updateRevenueChart;
    private bool _updateProductChart;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        OrdersYear = RevenueYear = DateTime.Today.Year;
        ProductsStart = new DateTime(DateTime.Today.Year, 1, 1);
        ProductsEnd = DateTime.Today;
        await LoadOrdersAsync();
        await LoadRevenueAsync();
        await LoadProductsAsync();
    }

    protected async Task LoadOrdersAsync()
    {
        Orders = await StatsService.GetOrdersByMonthAsync(OrdersYear);
        _updateOrdersChart = true;
    }

    protected async Task LoadRevenueAsync()
    {
        Revenue = await StatsService.GetRevenueByMonthAsync(RevenueYear);
        _updateRevenueChart = true;
    }

    protected async Task LoadProductsAsync()
    {
        ProductResult = await StatsService.GetProductStatsAsync(ProductsStart, ProductsEnd);
        _updateProductChart = true;
    }

    protected async Task OnOrdersYearChange(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var year))
        {
            OrdersYear = year;
            await LoadOrdersAsync();
        }
    }

    protected async Task OnRevenueYearChange(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var year))
        {
            RevenueYear = year;
            await LoadRevenueAsync();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_updateOrdersChart)
        {
            _updateOrdersChart = false;
            await JS.InvokeVoidAsync("updateOrdersChart", new { orders = Orders });
        }
        if (_updateRevenueChart)
        {
            _updateRevenueChart = false;
            await JS.InvokeVoidAsync("updateRevenueChart", new { revenue = Revenue });
        }
        if (_updateProductChart && ProductResult != null)
        {
            _updateProductChart = false;
            await JS.InvokeVoidAsync("updateProductChart", new
            {
                labels = ProductResult.ProductStats.Select(p => p.Name).ToArray(),
                data = ProductResult.ProductStats.Select(p => p.Quantity).ToArray(),
                topProduct = ProductResult.TopProductName
            });
        }
    }
}
