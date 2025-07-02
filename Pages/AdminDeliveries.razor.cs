using Microsoft.AspNetCore.Components;
using OnigiriShop.Data.Models;
using OnigiriShop.Data;

namespace OnigiriShop.Pages
{
    public class AdminDeliveriesBase : ComponentBase
    {
        [Inject] public DeliveryService DeliveryService { get; set; }

        public List<Delivery> Deliveries { get; set; } = new();
        public bool ShowModal { get; set; }
        public bool IsEdit { get; set; }
        public Delivery ModalModel { get; set; } = new();
        public string ModalTitle => IsEdit ? "Modifier la livraison" : "Ajouter une livraison";
        public bool IsBusy { get; set; }
        public DateTime? ModalDate { get; set; }
        public TimeOnly? ModalTime { get; set; }   // ← NOUVEAU
        public string ModalError { get; set; }
        public string SearchTerm { get; set; }
        public List<Delivery> FilteredDeliveries => string.IsNullOrWhiteSpace(SearchTerm)
            ? Deliveries
            : Deliveries.Where(d =>
                    (!string.IsNullOrEmpty(d.Place) && d.Place.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                    || (d.DeliveryAt.ToString("dd/MM/yyyy HH:mm").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                ).ToList();

        protected override async Task OnInitializedAsync()
        {
            await ReloadDeliveriesAsync();
        }

        public async Task ReloadDeliveriesAsync()
        {
            Deliveries = await DeliveryService.GetAllAsync();
            StateHasChanged();
        }

        public void ShowAddModal()
        {
            ModalModel = new Delivery
            {
                DeliveryAt = DateTime.Now.AddDays(1).Date.AddHours(12),
                IsRecurring = false
            };
            ModalDate = ModalModel.DeliveryAt.Date;
            ModalTime = TimeOnly.FromDateTime(ModalModel.DeliveryAt);
            IsEdit = false;
            ShowModal = true;
            ModalError = null;
        }

        public void EditDelivery(Delivery delivery)
        {
            ModalModel = new Delivery
            {
                Id = delivery.Id,
                Place = delivery.Place,
                DeliveryAt = delivery.DeliveryAt,
                IsRecurring = delivery.IsRecurring,
                RecurrenceRule = delivery.RecurrenceRule,
                Comment = delivery.Comment
            };
            ModalDate = ModalModel.DeliveryAt.Date;
            ModalTime = TimeOnly.FromDateTime(ModalModel.DeliveryAt);
            IsEdit = true;
            ShowModal = true;
            ModalError = null;
        }

        public void HideModal()
        {
            ShowModal = false;
            StateHasChanged();
        }

        public async Task HandleModalValid()
        {
            IsBusy = true;
            ModalError = null;

            if (!ModalDate.HasValue || !ModalTime.HasValue)
            {
                ModalError = "Merci de renseigner la date ET l'heure de livraison.";
                IsBusy = false;
                StateHasChanged();
                return;
            }

            ModalModel.DeliveryAt = ModalDate.Value.Date + ModalTime.Value.ToTimeSpan();

            if (IsEdit)
                await DeliveryService.UpdateAsync(ModalModel);
            else
                await DeliveryService.CreateAsync(ModalModel);

            IsBusy = false;
            HideModal();
            await ReloadDeliveriesAsync();
        }

        public bool ShowDeleteConfirm { get; set; }
        public Delivery DeleteModel { get; set; }

        public void ConfirmDeleteDelivery(Delivery delivery)
        {
            DeleteModel = delivery;
            ShowDeleteConfirm = true;
        }

        public void CancelDelete()
        {
            ShowDeleteConfirm = false;
            DeleteModel = null;
        }

        public async Task DeleteDeliveryConfirmed()
        {
            if (DeleteModel != null)
            {
                await DeliveryService.SoftDeleteAsync(DeleteModel.Id);
                await ReloadDeliveriesAsync();
            }
            CancelDelete();
        }
    }
}
