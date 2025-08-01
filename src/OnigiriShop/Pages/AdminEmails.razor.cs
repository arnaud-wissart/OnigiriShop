using Microsoft.AspNetCore.Components;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;

namespace OnigiriShop.Pages
{
    public class AdminEmailsBase : CustomComponentBase
    {
        [Inject] public EmailTemplateService EmailTemplateService { get; set; } = default!;
        [Inject] public SettingService SettingService { get; set; } = default!;
        protected List<EmailTemplate> TemplateTemplates { get; set; } = [];
        protected EmailTemplate TemplateModalModel { get; set; } = new();
        protected EmailTemplate? TemplateDeleteModel { get; set; }
        protected bool TemplateShowModal { get; set; }
        protected bool TemplateIsEdit { get; set; }
        protected bool TemplateShowDeleteConfirm { get; set; }
        protected bool TemplateIsBusy { get; set; }
        protected string? TemplateModalError { get; set; }
        protected string TemplateModalTitle => TemplateIsEdit ? "Modifier un template" : "Ajouter un template";
        protected EmailSettingsModel SettingsModel { get; set; } = new();
        protected bool SettingsBusy { get; set; }
        protected string? SettingsMessage { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await ReloadTemplatesAsync();
            await LoadSettingsAsync();
        }

        protected async Task ReloadTemplatesAsync()
        {
            TemplateTemplates = await EmailTemplateService.GetAllAsync();
            StateHasChanged();
        }

        protected void TemplateShowAddModal()
        {
            TemplateModalModel = new EmailTemplate();
            TemplateIsEdit = false;
            TemplateShowModal = true;
            TemplateModalError = null;
        }

        protected void TemplateEditTemplate(EmailTemplate t)
        {
            TemplateModalModel = new EmailTemplate
            {
                Id = t.Id,
                Name = t.Name,
                HtmlContent = t.HtmlContent,
                TextContent = t.TextContent
            };
            TemplateIsEdit = true;
            TemplateShowModal = true;
            TemplateModalError = null;
        }

        protected void TemplateHideModal()
        {
            TemplateShowModal = false;
            TemplateModalModel = new EmailTemplate();
            TemplateModalError = null;
        }

        protected async Task TemplateHandleModalValid()
        {
            TemplateIsBusy = true;
            TemplateModalError = null;
            if (string.IsNullOrWhiteSpace(TemplateModalModel.Name))
            {
                TemplateModalError = "Le nom est obligatoire.";
                TemplateIsBusy = false;
                StateHasChanged();
                return;
            }
            if (string.IsNullOrWhiteSpace(TemplateModalModel.HtmlContent))
            {
                TemplateModalError = "Le contenu HTML est obligatoire.";
                TemplateIsBusy = false;
                StateHasChanged();
                return;
            }

            if (TemplateIsEdit)
                await EmailTemplateService.UpdateAsync(TemplateModalModel);
            else
                await EmailTemplateService.CreateAsync(TemplateModalModel);

            TemplateIsBusy = false;
            TemplateHideModal();
            await ReloadTemplatesAsync();
        }

        protected void TemplateConfirmDelete(EmailTemplate t)
        {
            TemplateDeleteModel = t;
            TemplateShowDeleteConfirm = true;
        }

        protected void TemplateConfirmDeleteModal()
        {
            TemplateDeleteModel = TemplateModalModel;
            TemplateShowDeleteConfirm = true;
        }

        protected void TemplateCancelDelete()
        {
            TemplateDeleteModel = null;
            TemplateShowDeleteConfirm = false;
        }

        protected async Task TemplateDeleteConfirmed()
        {
            if (TemplateDeleteModel != null)
                await EmailTemplateService.DeleteAsync(TemplateDeleteModel.Id);

            TemplateShowDeleteConfirm = false;
            TemplateDeleteModel = null;
            TemplateHideModal();
            await ReloadTemplatesAsync();
        }

        protected async Task LoadSettingsAsync()
        {
            SettingsModel.ExpeditorEmail = await SettingService.GetValueAsync("ExpeditorEmail") ?? string.Empty;
            SettingsModel.ExpeditorName = await SettingService.GetValueAsync("ExpeditorName") ?? string.Empty;
            SettingsModel.InvitationSubject = await SettingService.GetValueAsync("InvitationSubject") ?? string.Empty;
            SettingsModel.InvitationIntro = await SettingService.GetValueAsync("InvitationIntro") ?? string.Empty;
            SettingsModel.PasswordResetSubject = await SettingService.GetValueAsync("PasswordResetSubject") ?? string.Empty;
            SettingsModel.PasswordResetIntro = await SettingService.GetValueAsync("PasswordResetIntro") ?? string.Empty;
            SettingsModel.OrderSubject = await SettingService.GetValueAsync("OrderSubject") ?? string.Empty;
            SettingsModel.Signature = await SettingService.GetValueAsync("Signature") ?? string.Empty;
            SettingsModel.AdminEmail = await SettingService.GetValueAsync("AdminEmail") ?? string.Empty;
            SettingsMessage = null;
            StateHasChanged();
        }

        protected async Task SaveSettingsAsync()
        {
            SettingsBusy = true;
            await SettingService.SetValueAsync("ExpeditorEmail", SettingsModel.ExpeditorEmail);
            await SettingService.SetValueAsync("ExpeditorName", SettingsModel.ExpeditorName);
            await SettingService.SetValueAsync("InvitationSubject", SettingsModel.InvitationSubject);
            await SettingService.SetValueAsync("InvitationIntro", SettingsModel.InvitationIntro);
            await SettingService.SetValueAsync("PasswordResetSubject", SettingsModel.PasswordResetSubject);
            await SettingService.SetValueAsync("PasswordResetIntro", SettingsModel.PasswordResetIntro);
            await SettingService.SetValueAsync("OrderSubject", SettingsModel.OrderSubject);
            await SettingService.SetValueAsync("Signature", SettingsModel.Signature);
            await SettingService.SetValueAsync("AdminEmail", SettingsModel.AdminEmail);
            SettingsBusy = false;
            SettingsMessage = "Paramètres enregistrés";
        }
    }
}
