using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;

namespace OnigiriShop.Pages
{
    public class AdminEmailsBase : CustomComponentBase
    {
        [Inject] public EmailTemplateService EmailTemplateService { get; set; } = default!;
        [Inject] public EmailVariationService EmailVariationService { get; set; } = default!;

        // Template properties
        protected List<EmailTemplate> TemplateTemplates { get; set; } = new();
        protected EmailTemplate TemplateModalModel { get; set; } = new();
        protected EmailTemplate TemplateDeleteModel { get; set; }
        protected bool TemplateShowModal { get; set; }
        protected bool TemplateIsEdit { get; set; }
        protected bool TemplateShowDeleteConfirm { get; set; }
        protected bool TemplateIsBusy { get; set; }
        protected string TemplateModalError { get; set; } = string.Empty;
        protected string TemplateModalTitle => TemplateIsEdit ? "Modifier un template" : "Ajouter un template";

        // Variation properties
        protected List<EmailVariation> VariationAllVariations { get; set; } = new();
        protected List<string> VariationCategories { get; set; } = new()
        {
            "Expeditor", "InvitationSubject", "InvitationIntro", "PasswordResetSubject",
            "PasswordResetIntro", "OrderSubject", "Signature"
        };
        protected Dictionary<string, string> VariationCategoryLabels = new()
        {
            ["Expeditor"] = "Expéditeurs (adresse + nom affiché)",
            ["InvitationSubject"] = "Sujets d'invitation",
            ["InvitationIntro"] = "Intros d'invitation",
            ["PasswordResetSubject"] = "Sujets de réinitialisation de mot de passe",
            ["PasswordResetIntro"] = "Intros de reset de mot de passe",
            ["OrderSubject"] = "Sujets de confirmation de commande",
            ["Signature"] = "Signatures"
        };
        protected EmailVariation VariationModalModel { get; set; } = new();
        protected EmailVariation VariationDeleteModel { get; set; }
        protected string VariationModalTitle => VariationIsEdit ? "Modifier une variation" : "Ajouter une variation";
        protected string VariationModalError { get; set; } = string.Empty;
        protected bool VariationShowModal { get; set; }
        protected bool VariationIsEdit { get; set; }
        protected bool VariationShowDeleteConfirm { get; set; }
        protected bool VariationIsBusy { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await ReloadTemplatesAsync();
            await ReloadVariationsAsync();
        }

        // Template methods
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

        // Variation methods
        protected async Task ReloadVariationsAsync()
        {
            VariationAllVariations = await EmailVariationService.GetAllAsync();
            StateHasChanged();
        }

        protected void VariationShowAddModal(string type)
        {
            VariationModalModel = new EmailVariation { Type = type };
            VariationIsEdit = false;
            VariationShowModal = true;
            VariationModalError = null;
        }

        protected void VariationEditVariation(EmailVariation v)
        {
            VariationModalModel = new EmailVariation
            {
                Id = v.Id,
                Type = v.Type,
                Name = v.Name,
                Value = v.Value,
                Extra = v.Extra
            };
            VariationIsEdit = true;
            VariationShowModal = true;
            VariationModalError = null;
        }

        protected void VariationHideModal()
        {
            VariationShowModal = false;
            VariationModalModel = new EmailVariation();
            VariationModalError = null;
        }

        protected async Task VariationHandleModalValid()
        {
            VariationIsBusy = true;
            VariationModalError = null;

            if (string.IsNullOrWhiteSpace(VariationModalModel.Value))
            {
                VariationModalError = "La valeur ne peut pas être vide.";
                VariationIsBusy = false;
                StateHasChanged();
                return;
            }
            if (VariationModalModel.Type == "Expeditor" && string.IsNullOrWhiteSpace(VariationModalModel.Extra))
            {
                VariationModalError = "Le nom affiché est obligatoire pour un expéditeur.";
                VariationIsBusy = false;
                StateHasChanged();
                return;
            }

            if (VariationIsEdit)
                await EmailVariationService.UpdateAsync(VariationModalModel);
            else
                await EmailVariationService.CreateAsync(VariationModalModel);

            VariationIsBusy = false;
            VariationHideModal();
            await ReloadVariationsAsync();
        }

        protected void VariationConfirmDelete(EmailVariation v)
        {
            VariationDeleteModel = v;
            VariationShowDeleteConfirm = true;
        }

        protected void VariationConfirmDeleteModal()
        {
            VariationDeleteModel = VariationModalModel;
            VariationShowDeleteConfirm = true;
        }

        protected void VariationCancelDelete()
        {
            VariationShowDeleteConfirm = false;
            VariationDeleteModel = null;
        }

        protected async Task VariationDeleteConfirmed()
        {
            if (VariationDeleteModel != null)
                await EmailVariationService.DeleteAsync(VariationDeleteModel.Id);

            VariationShowDeleteConfirm = false;
            VariationDeleteModel = null;
            VariationHideModal();
            await ReloadVariationsAsync();
        }
    }
}
