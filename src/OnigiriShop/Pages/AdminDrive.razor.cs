using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;

namespace OnigiriShop.Pages;

public class AdminDriveBase : CustomComponentBase
{
    [Inject] public RemoteDriveService RemoteDriveService { get; set; } = default!;
    [Inject] public HttpDatabaseBackupService BackupService { get; set; } = default!;
    [Inject] public IGoogleDriveService GoogleDriveService { get; set; } = default!;
    [Inject] public IOptions<BackupConfig> BackupOptions { get; set; } = default!;

    protected string DriveFolderId { get; set; } = string.Empty;
    protected string? Message { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        DriveFolderId = await RemoteDriveService.GetFolderIdAsync() ?? string.Empty;
    }

    protected async Task SaveAsync()
    {
        await RemoteDriveService.SetFolderIdAsync(DriveFolderId.Trim());
        Message = "Enregistré";
    }

    protected async Task BackupNowAsync()
    {
        if (!string.IsNullOrWhiteSpace(BackupOptions.Value.Endpoint))
            await BackupService.BackupAsync(BackupOptions.Value.Endpoint);
        if (!string.IsNullOrWhiteSpace(DriveFolderId))
            await GoogleDriveService.UploadBackupAsync(DriveFolderId);
        Message = "Sauvegarde effectuée";
    }
}
