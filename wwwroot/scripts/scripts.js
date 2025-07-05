window.onigiriAuth = {
    login: async function (email, password) {
        try {
            const response = await fetch("/api/auth/login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                credentials: "same-origin"
                , body: JSON.stringify({ email, password })
            });
            if (response.ok) {
                return { success: true };
            } else if (response.status === 401) {
                return { success: false, error: "Email ou mot de passe incorrect." };
            } else {
                return { success: false, error: `Erreur serveur (${response.status})` };
            }
        } catch (err) {
            return { success: false, error: "Erreur : " + err };
        }
    }
};
window.onigiriAuth.logout = async function () {
    try {
        const response = await fetch("/api/auth/logout", {
            method: "POST",
            credentials: "same-origin"
        });
        if (response.ok) {
            location.reload();
        } else {
            alert("Erreur lors de la déconnexion (" + response.status + ")");
        }
    } catch (err) {
        alert("Erreur lors de la déconnexion : " + err);
    }
};

window.bootstrapInterop = {
    showModal: function (selector) {
        var modalEl = document.querySelector(selector);
        if (modalEl) {
            var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            modal.show();
        }
    },
    hideModal: function (selector) {
        var modalEl = document.querySelector(selector);
        if (modalEl) {
            var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            modal.hide();
        }
    }
};
window.activateTooltips = () => {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
        var tooltip = bootstrap.Tooltip.getInstance(el);
        if (tooltip) tooltip.dispose();
    });

    document.querySelectorAll('.tooltip').forEach(function (el) {
        el.parentNode && el.parentNode.removeChild(el);
    });

    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
        bootstrap.Tooltip.getOrCreateInstance(el);
    });
};


window.onigiriCalendar = {
    init: function (dotNetHelper, weekStartDay) {
        // Détruire une ancienne instance si elle existe
        if (window.onigiriCalInstance) window.onigiriCalInstance.destroy();

        if (!window.onigiriLegendColors) {
            window.onigiriLegendColors = {
                ponctuelle: '#198754',
                recurrente: '#0dcaf0'
            };
        }

        // Nouvelle instance FullCalendar avec events fonction callback
        window.onigiriCalInstance = new FullCalendar.Calendar(document.getElementById('calendar'), {
            locale: 'fr',
            themeSystem: 'bootstrap5',
            initialView: 'dayGridMonth',
            firstDay: weekStartDay,
            height: 650,
            headerToolbar: {
                left: 'prev,next today',
                center: 'title',
                right: 'dayGridMonth,timeGridWeek,timeGridDay'
            },
            buttonText: {
                today: "Aujourd'hui",
                month: 'Mois',
                week: 'Semaine',
                day: 'Jour'
            },
            allDayText: 'Toute la journée',
            // ⬇️ Fonction callback dynamique côté Blazor
            events: async function(info, successCallback, failureCallback) {
                try {
                    // Demande à Blazor la liste des events pour cette plage
                    const events = await dotNetHelper.invokeMethodAsync(
                        'GetCalendarEventsForPeriod',
                        info.startStr,
                        info.endStr
                    );
                    successCallback(events);
                } catch (e) {
                    failureCallback(e);
                }
            },
            dateClick: function (info) { dotNetHelper.invokeMethodAsync('OnCalendarDateClick', info.dateStr); },
            eventClick: function (info) { dotNetHelper.invokeMethodAsync('OnCalendarEventClick', info.event.id); },
            eventContent: function (arg) {
                const event = arg.event;
                const heure = new Date(event.start).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
                const bgColor = event.backgroundColor || event.color || "#425caa";
                const txtColor = event.textColor || "#fff";
                return {
                    html: `
                        <div class="fc-event-bubble" style="background:${bgColor}; color:${txtColor};">
                            <span class="fc-event-hour">${heure}</span>
                            <span class="fc-event-title" title="${event.title}">${event.title}</span>
                        </div>
                    `
                };
            }
        });

        window.onigiriCalInstance.render();

        window.updateDeliveryColors();
    },
    updateEvents: function () {
        if (window.onigiriCalInstance) {
            window.onigiriCalInstance.refetchEvents();
            window.updateDeliveryColors();
        }
    }
};


window.onigiriCalendar.exists = function () {
    return !!document.getElementById('calendar');
};

window.onigiriColorModalOpenInput = function () {
    setTimeout(function() {
        var input = document.getElementById("colorModalInput");
        if (input) input.click();
    }, 100);
};

window.onigiriCalendar.setLegendColors = function (ponctuelle, recurrente) {
    window.onigiriLegendColors = {
        ponctuelle: ponctuelle,
        recurrente: recurrente
    };
    window.updateDeliveryColors();
};


// Modale sélecteur de couleur custom
window.openColorModal = function (type) {
    if (document.getElementById('onigiriColorModal')) return; // déjà affichée

    const modal = document.createElement('div');
    modal.className = "color-modal-overlay";
    modal.id = "onigiriColorModal";
    modal.innerHTML = `
        <div class="color-modal-dialog">
            <button class="color-modal-close" id="colorModalCloseBtn" aria-label="Fermer la fenêtre">&times;</button>
            <div class="color-modal-title">
                Sélectionnez une couleur pour <b>${type === 'ponctuelle' ? "Ponctuelle" : "Récurrente"}</b>
            </div>
            <input id="colorModalInput" type="color" value="${window.onigiriLegendColors[type]}" style="width:80px; height:80px; border:none; margin-bottom:1.6rem; box-shadow:0 0 8px #dee2e6;">
            <div class="color-modal-actions">
                <button class="btn btn-success" id="colorModalOkBtn">Valider</button>
            </div>
        </div>
    `;
    document.body.appendChild(modal);

    // Fermer avec la croix
    document.getElementById('colorModalCloseBtn').onclick = function () {
        document.body.removeChild(modal);
    };

    // Validation
    document.getElementById('colorModalOkBtn').onclick = function () {
        const val = document.getElementById('colorModalInput').value;
        window.onigiriLegendColors[type] = val;
        document.body.removeChild(modal);
        window.updateDeliveryColors();
    };

    // Focus sur input + simule le click pour ouvrir la palette direct
    setTimeout(() => {
        const input = document.getElementById("colorModalInput");
        input.focus();
        // Cette astuce déclenche l’ouverture de la palette (chrome, edge, opera… pas toujours sur Firefox)
        input.click();
    }, 120);

    // Empêche fermeture sur clic overlay
    modal.onclick = function (e) {
        if (e.target === modal) {
            // Rien, la modale ne se ferme pas ici
        }
    };
};



// Pour convertir rgb() -> #xxxxxx au cas où (protection)
function rgbToHex(color) {
    if (color.startsWith('#')) return color;
    const rgb = color.match(/\d+/g);
    if (!rgb) return '#198754';
    return (
        '#' +
        ((1 << 24) + (parseInt(rgb[0]) << 16) + (parseInt(rgb[1]) << 8) + parseInt(rgb[2]))
            .toString(16)
            .slice(1)
    );
}

// Mets à jour les couleurs des events selon le type
window.updateDeliveryColors = function () {
    if (!window.onigiriCalInstance) return;
    window.onigiriCalInstance.getEvents().forEach(ev => {
        // isRecurring est sur extendedProps
        const isRec = ev.extendedProps && ev.extendedProps.isRecurring;
        if (isRec !== undefined) {
            if (isRec)
                ev.setProp('backgroundColor', window.onigiriLegendColors.recurrente);
            else
                ev.setProp('backgroundColor', window.onigiriLegendColors.ponctuelle);
            ev.setProp('borderColor', '#dee2e6');
            ev.setProp('textColor', '#fff');
        }
    });
};