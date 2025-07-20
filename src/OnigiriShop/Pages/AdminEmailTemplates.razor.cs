using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OnigiriShop.Data.Models;
using OnigiriShop.Services;
using OnigiriShop.Infrastructure;

namespace OnigiriShop.Pages
{
    public class AdminEmailTemplatesBase : CustomComponentBase
    {
        [Inject] public EmailTemplateService EmailTemplateService { get; set; }

        protected List<EmailTemplate> Templates { get; set; } = new();
        protected EmailTemplate ModalModel { get; set; } = new();
        protected EmailTemplate? DeleteModel { get; set; }
        protected bool ShowModal { get; set; }
        protected bool IsEdit { get; set; }
        protected bool ShowDeleteConfirm { get; set; }
        protected bool IsBusy { get; set; }
        protected string? ModalError { get; set; }
        protected string ModalTitle => IsEdit ? "Modifier un template" : "Ajouter un template";

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await ReloadAsync();
        }

        protected async Task ReloadAsync()
        {
            Templates = await EmailTemplateService.GetAllAsync();
            StateHasChanged();
        }

        protected void ShowAddModal()
        {
            ModalModel = new EmailTemplate();
            IsEdit = false;
            ShowModal = true;
            ModalError = null;
        }

        protected void EditTemplate(EmailTemplate t)
        {
            ModalModel = new EmailTemplate
            {
                Id = t.Id,
                Name = t.Name,
                HtmlContent = t.HtmlContent,
                TextContent = t.TextContent
            };
            IsEdit = true;
            ShowModal = true;
            ModalError = null;
        }

        protected void HideModal()
        {
            ShowModal = false;
            ModalModel = new EmailTemplate();
            ModalError = null;
        }

        protected async Task HandleModalValid()
        {
            IsBusy = true;
            ModalError = null;
            if (string.IsNullOrWhiteSpace(ModalModel.Name))
            {
                ModalError = "Le nom est obligatoire.";
                IsBusy = false;
                StateHasChanged();
                return;
            }
            if (string.IsNullOrWhiteSpace(ModalModel.HtmlContent))
            {
                ModalError = "Le contenu HTML est obligatoire.";
                IsBusy = false;
                StateHasChanged();
                return;
            }

            if (IsEdit)
                await EmailTemplateService.UpdateAsync(ModalModel);
            else
                await EmailTemplateService.CreateAsync(ModalModel);

            IsBusy = false;
            HideModal();
            await ReloadAsync();
        }

        protected void ConfirmDelete(EmailTemplate t)
        {
            DeleteModel = t;
            ShowDeleteConfirm = true;
        }

        protected void ConfirmDeleteModal()
        {
            DeleteModel = ModalModel;
            ShowDeleteConfirm = true;
        }

        protected void CancelDelete()
        {
            DeleteModel = null;
            ShowDeleteConfirm = false;
        }

        protected async Task DeleteConfirmed()
        {
            if (DeleteModel != null)
                await EmailTemplateService.DeleteAsync(DeleteModel.Id);

            ShowDeleteConfirm = false;
            DeleteModel = null;
            HideModal();
            await ReloadAsync();
        }
    }
}