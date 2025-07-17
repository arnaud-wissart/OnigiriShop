using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;

namespace OnigiriShop.Pages
{
    public class AdminEmailVariationsBase : CustomComponentBase
    {
        [Inject] public EmailVariationService EmailVariationService { get; set; }

        protected List<EmailVariation> AllVariations { get; set; } = new();
        protected List<string> Categories { get; set; } = new()
        {
            "Expeditor", "InvitationSubject", "InvitationIntro", "PasswordResetSubject",
            "PasswordResetIntro", "OrderSubject", "Signature"
        };
        protected Dictionary<string, string> CategoryLabels = new()
        {
            ["Expeditor"] = "Expéditeurs (adresse + nom affiché)",
            ["InvitationSubject"] = "Sujets d'invitation",
            ["InvitationIntro"] = "Intros d'invitation",
            ["PasswordResetSubject"] = "Sujets de réinitialisation de mot de passe",
            ["PasswordResetIntro"] = "Intros de reset de mot de passe",
            ["OrderSubject"] = "Sujets de confirmation de commande",
            ["Signature"] = "Signatures"
        };

        protected EmailVariation ModalModel { get; set; } = new();
        protected EmailVariation DeleteModel { get; set; }
        protected string ModalTitle => IsEdit ? "Modifier une variation" : "Ajouter une variation";
        protected string ModalError { get; set; }
        protected bool ShowModal { get; set; }
        protected bool IsEdit { get; set; }
        protected bool ShowDeleteConfirm { get; set; }
        protected bool IsBusy { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            await ReloadAsync();
        }

        protected async Task ReloadAsync()
        {
            AllVariations = await EmailVariationService.GetAllAsync();
            StateHasChanged();
        }

        protected void ShowAddModal(string type)
        {
            ModalModel = new EmailVariation { Type = type };
            IsEdit = false;
            ShowModal = true;
            ModalError = null;
        }

        protected void EditVariation(EmailVariation v)
        {
            ModalModel = new EmailVariation
            {
                Id = v.Id,
                Type = v.Type,
                Name = v.Name,
                Value = v.Value,
                Extra = v.Extra
            };
            IsEdit = true;
            ShowModal = true;
            ModalError = null;
        }

        protected void HideModal()
        {
            ShowModal = false;
            ModalModel = new EmailVariation();
            ModalError = null;
        }

        protected async Task HandleModalValid()
        {
            IsBusy = true;
            ModalError = null;

            // Validation simple
            if (string.IsNullOrWhiteSpace(ModalModel.Value))
            {
                ModalError = "La valeur ne peut pas être vide.";
                IsBusy = false;
                StateHasChanged();
                return;
            }
            if (ModalModel.Type == "Expeditor" && string.IsNullOrWhiteSpace(ModalModel.Extra))
            {
                ModalError = "Le nom affiché est obligatoire pour un expéditeur.";
                IsBusy = false;
                StateHasChanged();
                return;
            }

            if (IsEdit)
                await EmailVariationService.UpdateAsync(ModalModel);
            else
                await EmailVariationService.CreateAsync(ModalModel);

            IsBusy = false;
            HideModal();
            await ReloadAsync();
        }

        protected void ConfirmDelete(EmailVariation v)
        {
            DeleteModel = v;
            ShowDeleteConfirm = true;
        }

        protected void ConfirmDeleteModal()
        {
            DeleteModel = ModalModel;
            ShowDeleteConfirm = true;
        }

        protected void CancelDelete()
        {
            ShowDeleteConfirm = false;
            DeleteModel = null;
        }

        protected async Task DeleteConfirmed()
        {
            if (DeleteModel != null)
                await EmailVariationService.DeleteAsync(DeleteModel.Id);

            ShowDeleteConfirm = false;
            DeleteModel = null;
            HideModal();
            await ReloadAsync();
        }
    }
}
