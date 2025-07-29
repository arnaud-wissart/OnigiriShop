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
    protected DateTime? LastBackupDate { get; set; }
    protected DateTime? FilterStart { get; set; }
    protected DateTime? FilterEnd { get; set; }
    protected int MaxLines { get; set; } = 1000;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        LogFiles = MaintenanceService.GetLogFiles().ToList();
        SelectedLogFile = LogFiles.FirstOrDefault();
        FilterEnd = DateTime.Now;
        FilterStart = DateTime.Now.AddHours(-1);
        await LoadLogAsync();
        LastBackupDate = MaintenanceService.GetLastBackupDate();
    }

    protected async Task OnLogFileChanged(ChangeEventArgs e)
    {
        SelectedLogFile = e.Value?.ToString();
        await LoadLogAsync();
    }

    private async Task LoadLogAsync()
    {
        if (!string.IsNullOrEmpty(SelectedLogFile))
            LogContent = await MaintenanceService.ReadLogAsync(
                SelectedLogFile,
                FilterStart,
                FilterEnd,
                MaxLines);
        else
            LogContent = string.Empty;
        StateHasChanged();
    }

    protected async Task OnDbSelected(InputFileChangeEventArgs e)
    {
        DbFile = e.FileCount > 0 ? e.File : null;
        if (DbFile != null)
            await ReplaceDatabaseAsync();
    }

    protected async Task ApplyFilterAsync() => await LoadLogAsync();

    protected async Task ReplaceDatabaseAsync()
    {
        if (DbFile == null) return;
        IsBusy = true;
        await MaintenanceService.ReplaceDatabaseAsync(DbFile.OpenReadStream());
        DbFile = null;
        IsBusy = false;
    }

    protected async Task DownloadDatabaseAsync()
    {
        var bytes = await MaintenanceService.GetDatabaseBytesAsync();
        var base64 = Convert.ToBase64String(bytes);
        await JS.InvokeVoidAsync("downloadFileFromBytes", "OnigiriShop.db", base64);
    }

    protected async Task TriggerDbUpload() => await JS.InvokeVoidAsync("triggerFileInput", "dbUpload");
}
