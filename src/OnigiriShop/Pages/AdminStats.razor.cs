using Microsoft.AspNetCore.Components;
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

    protected async Task LoadAsync() => Result = await StatsService.GetStatsAsync(StartDate, EndDate);
}
