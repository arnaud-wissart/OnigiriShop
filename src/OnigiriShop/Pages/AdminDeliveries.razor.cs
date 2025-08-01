using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;

namespace OnigiriShop.Pages
{
    public class AdminDeliveriesBase : CustomComponentBase
    {
        [Inject] public UserPreferenceService UserPreferenceService { get; set; } = default!;
        [Inject] public DeliveryService DeliveryService { get; set; } = default!;
        [Inject] public DeliveryCalendarService CalendarService { get; set; } = default!;
        [Inject] public IOptions<CalendarSettings> CalendarConfig { get; set; } = default!;
        public enum LegendType { Ponctuelle, Recurrente }
        public string? CouleurPonctuelle { get; set; }
        public string? CouleurRecurrente { get; set; }

        protected bool ShowColorModal;
        protected string? SelectedColor;
        protected string ModalColorTitle = "Choisissez la couleur";
        private LegendType TypeEnEdition;
        private bool colorModalJustOpened = false;
         
        protected UserPreferences? UserPreferences;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            objRef = DotNetObjectReference.Create(this);
            await ReloadDeliveriesAsync();

            UserPreferences = await UserPreferenceService.GetUserPreferencesAsync(UserId);

            CouleurPonctuelle = UserPreferences.CouleurPonctuelle ??= "#198754";
            CouleurRecurrente = UserPreferences.CouleurRecurrente ??= "#0dcaf0";
        }

        public async Task ShowColorModalFor(LegendType type)
        {
            await JS.InvokeVoidAsync("closeAllTooltips");
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

        public void OnColorChange(ChangeEventArgs e) => SelectedColor = e.Value?.ToString() ?? "#198754";

        public async Task ValidateColorAsync()
        {
            if (TypeEnEdition == LegendType.Ponctuelle)
            {
                CouleurPonctuelle = SelectedColor;
                UserPreferences!.CouleurPonctuelle = CouleurPonctuelle!;
            }
            else
            {
                CouleurRecurrente = SelectedColor;
                UserPreferences!.CouleurRecurrente = CouleurRecurrente!;
            }

            await UserPreferenceService.SaveUserPreferencesAsync(UserId, UserPreferences);

            await JS.InvokeVoidAsync("onigiriCalendar.setLegendColors", CouleurPonctuelle, CouleurRecurrente);

            StateHasChanged();

            await JS.InvokeVoidAsync("activateTooltips");

            ShowColorModal = false;
        }

        public List<Delivery> Deliveries { get; set; } = [];
        public bool ShowModal { get; set; }
        public bool ShowDeleteConfirm { get; set; }
        public bool IsEdit { get; set; }
        public bool IsCalendarModal { get; set; }
        public Delivery ModalModel { get; set; } = new();
        public Delivery? DeleteModel { get; set; }
        public string ModalTitle => IsEdit ? "Modifier la livraison" : "Ajouter une livraison";
        public bool IsBusy { get; set; }
        public DateTime? ModalDate { get; set; }
        public TimeOnly? ModalTime { get; set; }
        public string? ModalError { get; set; }
        public string? SearchTerm { get; set; }
        public int WeekStartDay => CalendarConfig?.Value?.FirstDayOfWeek ?? 1;

        public List<Delivery> FilteredDeliveries =>
            string.IsNullOrWhiteSpace(SearchTerm)
            ? Deliveries
            : Deliveries.Where(d =>
                (!string.IsNullOrEmpty(d.Place) && d.Place.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                d.DeliveryAt.ToString("dd/MM/yyyy HH:mm").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
            ).ToList();

        protected DotNetObjectReference<AdminDeliveriesBase>? objRef;
        private bool calendarInitialized = false;

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
            var events = CalendarService.BuildCalendarEvents(Deliveries, periodStart, periodEnd, CouleurPonctuelle!, CouleurRecurrente!);
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

            if (ModalModel.DeliveryAt < DateTime.Now)
            {
                ModalError = "La date de livraison doit être dans le futur.";
                IsBusy = false;
                StateHasChanged();
                return;
            }

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

        [JSInvokable]
        public Task OnCalendarEventClick(string eventId)
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
            return Task.CompletedTask;
        }

        [JSInvokable]
        public Task OnCalendarDateClick(string dateStr)
        {
            if (DateTime.TryParse(dateStr, out var date))
            {
                ShowAddModal();
                ModalDate = date.Date;
                ModalTime = date.Hour == 0 && date.Minute == 0 ? new TimeOnly(12, 0) : new TimeOnly(date.Hour, date.Minute);
                StateHasChanged();
            }
            return Task.CompletedTask;
        }

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
            var periodStart = DateTime.Parse(startIso);
            var periodEnd = DateTime.Parse(endIso);

            var events = CalendarService.BuildCalendarEvents(Deliveries, periodStart, periodEnd, CouleurPonctuelle!, CouleurRecurrente!);
            return Task.FromResult(events);
        }
    }
}
