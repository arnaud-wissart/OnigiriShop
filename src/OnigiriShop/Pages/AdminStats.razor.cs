using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;

namespace OnigiriShop.Pages;

public class AdminStatsBase : CustomComponentBase
{
    [Inject] public StatsService StatsService { get; set; } = default!;
    protected DateTime? StartDate { get; set; }
    protected DateTime? EndDate { get; set; }
    protected StatsResult? Result { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        StartDate = DateTime.Today.AddMonths(-1);
        EndDate = DateTime.Today;
        await LoadAsync();
    }

    protected async Task LoadAsync()
    {
        Result = await StatsService.GetStatsAsync(StartDate, EndDate);
        if (Result != null)
        {
            await JS.InvokeVoidAsync("updateStatsChart", new
            {
                totalOrders = Result.TotalOrders,
                uniqueCustomers = Result.UniqueCustomers,
                totalRevenue = Result.TotalRevenue
            });
        }
    }
}
