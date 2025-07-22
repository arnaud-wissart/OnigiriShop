using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;

namespace OnigiriShop.Pages;

public class AdminLogsBase : CustomComponentBase
{
    [Inject] public MaintenanceService MaintenanceService { get; set; } = default!;

    protected List<string?> LogFiles { get; set; } = [];
    protected string? SelectedLogFile { get; set; }
    protected string LogContent { get; set; } = string.Empty;
    protected IBrowserFile? DbFile { get; set; }
    protected bool IsBusy { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        LogFiles = MaintenanceService.GetLogFiles().ToList();
        SelectedLogFile = LogFiles.FirstOrDefault();
        await LoadLogAsync();
    }

    protected async Task OnLogFileChanged(ChangeEventArgs e)
    {
        SelectedLogFile = e.Value?.ToString();
        await LoadLogAsync();
    }

    private async Task LoadLogAsync()
    {
        if (!string.IsNullOrEmpty(SelectedLogFile))
            LogContent = await MaintenanceService.ReadLogAsync(SelectedLogFile);
        else
            LogContent = string.Empty;
        StateHasChanged();
    }

    protected void OnDbSelected(InputFileChangeEventArgs e) => DbFile = e.FileCount > 0 ? e.File : null;

    protected async Task ReplaceDatabaseAsync()
    {
        if (DbFile == null) return;
        IsBusy = true;
        await MaintenanceService.ReplaceDatabaseAsync(DbFile.OpenReadStream());
        IsBusy = false;
    }

    protected async Task DownloadDatabaseAsync()
    {
        var bytes = await MaintenanceService.GetDatabaseBytesAsync();
        var base64 = Convert.ToBase64String(bytes);
        await JS.InvokeVoidAsync("downloadFileFromBytes", "OnigiriShop.db", base64);
    }
}