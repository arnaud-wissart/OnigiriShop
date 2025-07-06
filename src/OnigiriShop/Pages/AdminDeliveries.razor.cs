using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using OnigiriShop.Data;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;

namespace OnigiriShop.Pages
{
    public class AdminDeliveriesBase : ComponentBase
    {
        [Inject] public DeliveryService DeliveryService { get; set; }
        [Inject] public IJSRuntime JS { get; set; }
        [Inject] public IOptions<CalendarSettings> CalendarConfig { get; set; }
        public enum LegendType { Ponctuelle, Recurrente }
        public string CouleurPonctuelle { get; set; } = "#198754";  // Vert
        public string CouleurRecurrente { get; set; } = "#0dcaf0";  // Bleu

        protected bool ShowColorModal;
        protected string SelectedColor = "#0d6efd";
        protected string ModalColorTitle = "Choisissez la couleur";
        private LegendType TypeEnEdition;
        private bool colorModalJustOpened = false;

        public void ShowColorModalFor(LegendType type)
        {
            TypeEnEdition = type;
            if (type == LegendType.Ponctuelle)
                SelectedColor = CouleurPonctuelle;
            else
                SelectedColor = CouleurRecurrente;

            ModalColorTitle = type == LegendType.Ponctuelle
                ? "Changer la couleur Ponctuelle"
                : "Changer la couleur Récurrente";

            ShowColorModal = true;
            colorModalJustOpened = true;
            StateHasChanged();
        }


        public void HideColorModal()
        {
            ShowColorModal = false;
            StateHasChanged();
        }

        public void OnColorChange(ChangeEventArgs e)
        {
            SelectedColor = e.Value?.ToString() ?? "#198754";
        }
        public async Task ValidateColorAsync()
        {
            if (TypeEnEdition == LegendType.Ponctuelle)
                CouleurPonctuelle = SelectedColor;
            else
                CouleurRecurrente = SelectedColor;

            await JS.InvokeVoidAsync("onigiriCalendar.setLegendColors", CouleurPonctuelle, CouleurRecurrente);

            StateHasChanged();

            await JS.InvokeVoidAsync("activateTooltips");

            ShowColorModal = false;
        }

        public List<Delivery> Deliveries { get; set; } = new();
        public bool ShowModal { get; set; }
        public bool ShowDeleteConfirm { get; set; }
        public bool IsEdit { get; set; }
        public bool IsCalendarModal { get; set; }
        public Delivery ModalModel { get; set; } = new();
        public Delivery DeleteModel { get; set; }
        public string ModalTitle => IsEdit ? "Modifier la livraison" : "Ajouter une livraison";
        public bool IsBusy { get; set; }
        public DateTime? ModalDate { get; set; }
        public TimeOnly? ModalTime { get; set; }
        public string ModalError { get; set; }
        public string SearchTerm { get; set; }
        public int WeekStartDay => CalendarConfig?.Value?.FirstDayOfWeek ?? 1;

        // Pour la checkbox
        public bool ModalIsRecurring
        {
            get => ModalModel.IsRecurring;
            set
            {
                ModalModel.IsRecurring = value;
                if (value)
                {
                    ModalModel.RecurrenceFrequency ??= RecurrenceFrequency.Week;
                    ModalModel.RecurrenceInterval ??= 1;
                }
                else
                {
                    ModalModel.RecurrenceFrequency = null;
                    ModalModel.RecurrenceInterval = null;
                }
            }
        }

        public List<Delivery> FilteredDeliveries =>
            string.IsNullOrWhiteSpace(SearchTerm)
            ? Deliveries
            : Deliveries.Where(d =>
                (!string.IsNullOrEmpty(d.Place) && d.Place.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                d.DeliveryAt.ToString("dd/MM/yyyy HH:mm").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
            ).ToList();

        protected DotNetObjectReference<AdminDeliveriesBase> objRef;
        private bool calendarInitialized = false;

        protected void ShowColorPicker(string currentColor, string title)
        {
            SelectedColor = currentColor;
            ModalColorTitle = title;
            ShowColorModal = true;
        }

        protected void ValidateColor()
        {
            // TODO : Appliquer la couleur là où tu veux (ex: met à jour la légende, etc)
            ShowColorModal = false;
            // Ex : LégendePonctuelleColor = SelectedColor;
        }

        void CloseColorModal()
        {
            ShowColorModal = false;
        }

        protected override async Task OnInitializedAsync()
        {
            objRef = DotNetObjectReference.Create(this);
            await ReloadDeliveriesAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            var exists = await JS.InvokeAsync<bool>("onigiriCalendar.exists");
            if (!exists) return;

            if (!calendarInitialized)
            {
                await JS.InvokeVoidAsync("onigiriCalendar.init", objRef, WeekStartDay);
                calendarInitialized = true;
            }

            if (ShowColorModal && colorModalJustOpened)
            {
                colorModalJustOpened = false;
                await JS.InvokeVoidAsync("onigiriColorModalOpenInput");
            }

            await JS.InvokeVoidAsync("activateTooltips");
        }

        // ==================== CRUD =======================

        public async Task ReloadDeliveriesAsync()
        {
            Deliveries = await DeliveryService.GetAllAsync();
            await RefreshCalendarAsync();
            StateHasChanged();
        }

        public async Task RefreshCalendarAsync()
        {
            var periodStart = DateTime.Now.Date.AddYears(-2);
            var periodEnd = DateTime.Now.Date.AddYears(20);
            var events = BuildCalendarEvents(Deliveries, periodStart, periodEnd);
            await JS.InvokeVoidAsync("onigiriCalendar.updateEvents", events);
        }

        public void ShowAddModal()
        {
            ModalModel = new Delivery
            {
                DeliveryAt = DateTime.Now.AddDays(1).Date.AddHours(12),
                IsRecurring = false,
                RecurrenceFrequency = RecurrenceFrequency.Week,
                RecurrenceInterval = 1
            };
            ModalDate = ModalModel.DeliveryAt.Date;
            ModalTime = TimeOnly.FromDateTime(ModalModel.DeliveryAt);
            IsEdit = false;
            IsCalendarModal = false;
            ShowModal = true;
            ModalError = null;
        }

        public void EditDelivery(Delivery delivery, bool fromCalendar = false)
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
            IsCalendarModal = fromCalendar;
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

            if (string.IsNullOrWhiteSpace(ModalModel.Place))
            {
                ModalError = "Le lieu est obligatoire.";
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

            var exists = Deliveries.Any(d =>
                d.Id != ModalModel.Id &&
                d.Place.Equals(ModalModel.Place?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                d.DeliveryAt.Date == ModalModel.DeliveryAt.Date &&
                d.DeliveryAt.Hour == ModalModel.DeliveryAt.Hour &&
                d.DeliveryAt.Minute == ModalModel.DeliveryAt.Minute);

            if (exists)
            {
                ModalError = "Une livraison existe déjà à ce lieu, à la même date et heure.";
                IsBusy = false;
                StateHasChanged();
                return;
            }

            if (IsEdit)
                await DeliveryService.UpdateAsync(ModalModel);
            else
                await DeliveryService.CreateAsync(ModalModel);

            IsBusy = false;
            HideModal();
            await ReloadDeliveriesAsync();
            await JS.InvokeVoidAsync("onigiriCalendar.updateEvents");
        }

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
            ShowModal = false;
            await JS.InvokeVoidAsync("onigiriCalendar.updateEvents");
            StateHasChanged();
        }

        // Occurrences pour calendrier (uniquement !)
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

            // Protection anti-boucle infinie !
            int maxCount = 1000, count = 0;
            while (current <= to && count++ < maxCount)
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

        // ========== JSInvokable (pour clics) =============

        [JSInvokable]
        public async Task OnCalendarEventClick(string eventId)
        {
            var idPart = eventId.Split('_')[0];
            if (int.TryParse(idPart, out int deliveryId))
            {
                var delivery = Deliveries.FirstOrDefault(d => d.Id == deliveryId);
                if (delivery != null)
                {
                    EditDelivery(delivery, fromCalendar: true);
                    StateHasChanged();
                }
            }
        }

        [JSInvokable]
        public async Task OnCalendarDateClick(string dateStr)
        {
            if (DateTime.TryParse(dateStr, out var date))
            {
                ShowAddModal();
                ModalDate = date.Date;
                ModalTime = date.Hour == 0 && date.Minute == 0 ? new TimeOnly(12, 0) : new TimeOnly(date.Hour, date.Minute);
                StateHasChanged();
            }
        }

        // ========== Helper pour Razor ============
        public string GetRecurrenceLabel(Delivery d)
        {
            if (!d.IsRecurring || !d.RecurrenceFrequency.HasValue || !d.RecurrenceInterval.HasValue) return "";
            var freq = d.RecurrenceFrequency switch
            {
                RecurrenceFrequency.Day => "jour",
                RecurrenceFrequency.Week => "semaine",
                RecurrenceFrequency.Month => "mois",
                _ => "?"
            };
            return $"tous les {d.RecurrenceInterval} {freq}{(d.RecurrenceInterval > 1 ? "s" : "")}";
        }

        [JSInvokable]
        public Task<List<CalendarEvent>> GetCalendarEventsForPeriod(string startIso, string endIso)
        {
            // Parse les dates depuis FullCalendar
            var periodStart = DateTime.Parse(startIso);
            var periodEnd = DateTime.Parse(endIso);

            var events = BuildCalendarEvents(Deliveries, periodStart, periodEnd);
            return Task.FromResult(events);
        }

        public List<CalendarEvent> BuildCalendarEvents(List<Delivery> deliveries, DateTime periodStart, DateTime periodEnd)
        {
            var list = new List<CalendarEvent>();
            foreach (var delivery in deliveries)
            {
                var occs = GetOccurrences(delivery, periodStart, periodEnd);
                foreach (var dt in occs)
                {
                    list.Add(new CalendarEvent
                    {
                        id = $"{delivery.Id}_{dt:yyyyMMddHHmm}",
                        title = $"{delivery.Place}{(delivery.IsRecurring ? " (récurrente)" : "")}",
                        start = dt.ToString("yyyy-MM-ddTHH:mm:ss"),
                        end = dt.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                        deliveryId = delivery.Id,
                        isRecurring = delivery.IsRecurring,
                        color = delivery.IsRecurring ? CouleurRecurrente : CouleurPonctuelle,
                        textColor = "#fff"
                    });
                }
            }
            return list;
        }

    }

    public class CalendarEvent
    {
        public string id { get; set; }
        public string title { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public int deliveryId { get; set; }
        public bool isRecurring { get; set; }
        public string color { get; set; }
        public string textColor { get; set; }
    }
}
