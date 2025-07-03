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
        public TimeOnly? ModalTime { get; set; }
        public RecurrenceFrequency? ModalFrequency
        {
            get => (RecurrenceFrequency?)ModalModel.RecurrenceFrequency;
            set => ModalModel.RecurrenceFrequency = value;
        }
        public int? ModalInterval
        {
            get => ModalModel.RecurrenceInterval;
            set => ModalModel.RecurrenceInterval = value;
        }
        public string ModalError { get; set; }
        public string SearchTerm { get; set; }

        public List<(Delivery, DateTime)> FilteredOccurrences
        {
            get
            {
                var results = new List<(Delivery, DateTime)>();
                var periodStart = DateTime.Now.Date.AddDays(-7);
                var periodEnd = DateTime.Now.Date.AddDays(31);
                foreach (var delivery in Deliveries)
                {
                    var occs = GetOccurrences(delivery, periodStart, periodEnd);
                    foreach (var dt in occs)
                    {
                        if (string.IsNullOrWhiteSpace(SearchTerm) ||
                            (delivery.Place?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                            dt.ToString("dd/MM/yyyy HH:mm").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add((delivery, dt));
                        }
                    }
                }
                return results.OrderBy(x => x.Item2).ToList();
            }
        }

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
                IsRecurring = false,
                RecurrenceFrequency = null,
                RecurrenceInterval = null
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
                RecurrenceFrequency = delivery.RecurrenceFrequency,
                RecurrenceInterval = delivery.RecurrenceInterval,
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

            if (ModalModel.IsRecurring)
            {
                if (!ModalModel.RecurrenceFrequency.HasValue)
                {
                    ModalError = "Merci de renseigner la fréquence de récurrence.";
                    IsBusy = false;
                    StateHasChanged();
                    return;
                }
                if (!ModalModel.RecurrenceInterval.HasValue || ModalModel.RecurrenceInterval < 1)
                {
                    ModalError = "Merci d'indiquer un intervalle (>0) pour la récurrence.";
                    IsBusy = false;
                    StateHasChanged();
                    return;
                }
            }
            else
            {
                ModalModel.RecurrenceFrequency = null;
                ModalModel.RecurrenceInterval = null;
            }

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

        // Génére les occurrences d’une livraison récurrente pour une période donnée
        public static List<DateTime> GetOccurrences(Delivery delivery, DateTime from, DateTime to)
        {
            var dates = new List<DateTime>();
            if (!delivery.IsRecurring || !delivery.RecurrenceFrequency.HasValue || !delivery.RecurrenceInterval.HasValue)
            {
                if (delivery.DeliveryAt >= from && delivery.DeliveryAt <= to)
                    dates.Add(delivery.DeliveryAt);
                return dates;
            }

            DateTime current = delivery.DeliveryAt;
            int interval = delivery.RecurrenceInterval.Value;

            while (current <= to)
            {
                if (current >= from)
                    dates.Add(current);

                current = delivery.RecurrenceFrequency switch
                {
                    RecurrenceFrequency.Day => current.AddDays(interval),
                    RecurrenceFrequency.Week => current.AddDays(7 * interval),
                    RecurrenceFrequency.Month => current.AddMonths(interval),
                    _ => current
                };
            }
            return dates;
        }
    }
}
