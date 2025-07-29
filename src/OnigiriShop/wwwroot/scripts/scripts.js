﻿// ==================== AUTH (Login / Logout) ====================

window.onigiriAuth = {
    /**
     * Authentifie un utilisateur via email/mot de passe.
     * @param {string} email 
     * @param {string} password 
     * @returns {Promise<{success: boolean, error?: string}>}
     */
    login: async function (email, password) {
        try {
            const response = await fetch("/api/auth/login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                credentials: "same-origin",
                body: JSON.stringify({ email, password })
            });
            if (response.ok) return { success: true };
            if (response.status === 401) return { success: false, error: "Email ou mot de passe incorrect." };
            return { success: false, error: `Erreur serveur (${response.status})` };
        } catch (err) {
            return { success: false, error: "Erreur : " + err };
        }
    },

    /**
     * Déconnecte l'utilisateur et redirige si succès.
     * @param {string} redirectUrl 
     */
    logout: async function (redirectUrl = "/") {
        try {
            const response = await fetch("/api/auth/logout", {
                method: "POST",
                credentials: "same-origin"
            });
            if (response.ok) {
                window.location.href = redirectUrl;
            } else {
                alert("Erreur lors de la déconnexion (" + response.status + ")");
            }
        } catch (err) {
            alert("Erreur lors de la déconnexion : " + err);
        }
    },


    /**
     * Rafraîchit la session utilisateur côté serveur.
     * @returns {Promise<boolean>}
     */
    refreshSession: async function () {
        try {
            const response = await fetch("/api/auth/refresh", {
                method: "POST",
                credentials: "same-origin"
            });
            return response.ok;
        } catch (err) {
            return false;
        }
    }
};
// ==================== Bootstrap Interop / Tooltips ====================

window.bootstrapInterop = {
    /**
     * Affiche une modale Bootstrap (par sélecteur CSS).
     * @param {string} selector 
     */
    showModal: function (selector) {
        if (window.closeAllTooltips) window.closeAllTooltips();
        var modalEl = document.querySelector(selector);
        if (modalEl) {
            var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            modal.show();
        }
    },
    /**
     * Masque une modale Bootstrap (par sélecteur CSS).
     * @param {string} selector 
     */
    hideModal: function (selector) {
        var modalEl = document.querySelector(selector);
        if (modalEl) {
            var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            modal.hide();
        }
    }
};

/**
 * Réactive tous les tooltips Bootstrap.
 */
window.activateTooltips = function () {
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
window.closeAllTooltips = function () {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
        var tooltip = bootstrap.Tooltip.getInstance(el);
        if (tooltip) tooltip.hide();
    });
};


// ==================== Calendar (FullCalendar) ====================

window.onigiriCalendar = {
    /**
     * Initialise FullCalendar avec .NET helper.
     */
    init: function (dotNetHelper, weekStartDay) {
        if (window.onigiriCalInstance) window.onigiriCalInstance.destroy();

        // Valeurs par défaut pour les couleurs de légende
        if (!window.onigiriLegendColors) {
            window.onigiriLegendColors = {
                ponctuelle: '#198754',
                recurrente: '#0dcaf0'
            };
        }

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
            datesSet: setFullCalendarTooltips,
            events: async function (info, successCallback, failureCallback) {
                try {
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
            dateClick: info => dotNetHelper.invokeMethodAsync('OnCalendarDateClick', info.dateStr),
            eventClick: info => dotNetHelper.invokeMethodAsync('OnCalendarEventClick', info.event.id),
            eventContent: function (arg) {
                const event = arg.event;
                const heure = new Date(event.start).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
                const bgColor = event.backgroundColor || event.color || "#425caa";
                const txtColor = event.textColor || "#fff";
                return {
                    html: `
                        <div class="fc-event-bubble"
                             data-bs-toggle="tooltip"
                             data-bs-placement="top"
                             title="${event.title}"
                             style="background:${bgColor}; color:${txtColor}; display: flex; align-items: center; gap: 0.45em; padding: 0.1em 0.7em;">
                            <span class="fc-event-hour" style="font-size:0.98em;">${heure}</span>
                            <span class="fc-event-title" style="font-size:0.98em;">${event.title}</span>
                        </div>
                    `
                };
            }
        });

        window.onigiriCalInstance.render();
        setFullCalendarTooltips();
        window.updateDeliveryColors();
    },

    updateEvents: function () {
        if (window.onigiriCalInstance) {
            window.onigiriCalInstance.refetchEvents();
            window.updateDeliveryColors();
        }
    },

    exists: function () {
        return !!document.getElementById('calendar');
    },

    setLegendColors: function (ponctuelle, recurrente) {
        window.onigiriLegendColors = { ponctuelle, recurrente };
        window.updateDeliveryColors();
    }
};

/**
 * Déclenche l'ouverture du color picker custom.
 */
window.onigiriColorModalOpenInput = function () {
    setTimeout(function () {
        var input = document.getElementById("colorModalInput");
        if (input) input.click();
    }, 100);
};

// ==================== Color Modal / Utils ====================

/**
 * Ouvre une modale de sélection de couleur pour le calendrier.
 * @param {"ponctuelle"|"recurrente"} type 
 */
window.openColorModal = function (type) {
    if (window.closeAllTooltips) window.closeAllTooltips();
    if (document.getElementById('onigiriColorModal')) return;

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
    if (window.activateTooltips) window.activateTooltips();

    document.getElementById('colorModalCloseBtn').onclick = function () {
        document.body.removeChild(modal);
    };
    document.getElementById('colorModalOkBtn').onclick = function () {
        const val = document.getElementById('colorModalInput').value;
        window.onigiriLegendColors[type] = val;
        document.body.removeChild(modal);
        window.updateDeliveryColors();
        if (window.activateTooltips) window.activateTooltips();
    };

    // Focus sur input + ouverture auto du picker (Chrome/Edge/Opera)
    setTimeout(() => {
        const input = document.getElementById("colorModalInput");
        input.focus();
        input.click();
    }, 120);

    // Empêche fermeture sur clic overlay
    modal.onclick = function (e) { if (e.target === modal) {/* pas de close */ } };
};

/**
 * Mets à jour la couleur des events du calendrier selon leur type.
 */
window.updateDeliveryColors = function () {
    if (!window.onigiriCalInstance) return;
    window.onigiriCalInstance.getEvents().forEach(ev => {
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

/**
 * Convertit une couleur rgb() en hexa.
 * @param {string} color 
 * @returns {string}
 */
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

/**
 * Applique les tooltips custom sur FullCalendar (FR).
 */
function setFullCalendarTooltips() {
    const tooltips = [
        ['.fc-dayGridMonth-button', 'Vue Mois'],
        ['.fc-timeGridWeek-button', 'Vue Semaine'],
        ['.fc-timeGridDay-button', 'Vue Jour'],
        ['.fc-listMonth-button', 'Vue Liste'],
        ['.fc-today-button', "Aujourd'hui"],
        ['.fc-prev-button', 'Mois précédent'],
        ['.fc-next-button', 'Mois suivant']
    ];
    tooltips.forEach(([selector, text]) => {
        document.querySelectorAll(selector).forEach(btn => {
            btn.removeAttribute('title');
            btn.setAttribute('data-bs-toggle', 'tooltip');
            btn.setAttribute('data-bs-placement', 'bottom');
            btn.setAttribute('title', text);
        });
    });
    if (window.bootstrap && window.bootstrap.Tooltip) {
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
            const tooltip = bootstrap.Tooltip.getInstance(el);
            if (tooltip) tooltip.dispose();
            new bootstrap.Tooltip(el);
        });
    }
}


// ==================== Utilitaires divers ====================

/**
 * Focus un élément par son name (input, etc.)
 * @param {string} name 
 */
window.focusElementByName = function (name) {
    var el = document.querySelector('[name="' + name + '"]');
    if (el) { el.focus(); if (el.select) el.select(); }
};

window.downloadFileFromText = (filename, text) => {
    const element = document.createElement('a');
    element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(text));
    element.setAttribute('download', filename);
    document.body.appendChild(element);
    element.click();
    document.body.removeChild(element);
};

window.downloadFileFromBytes = (filename, base64) => {
    const element = document.createElement('a');
    element.href = 'data:application/octet-stream;base64,' + base64;
    element.download = filename;
    document.body.appendChild(element);
    element.click();
    document.body.removeChild(element);
};

window.triggerFileInput = id => {
    const el = document.getElementById(id);
    if (el) el.click();
};

// ==================== HTML Editor (Quill) ====================

window.initHtmlEditor = function (id, dotNetHelper, value) {
    if (!window.quillEditors) window.quillEditors = {};
    const container = document.getElementById(id);
    if (!container || !window.Quill) return;
    const editor = new Quill(container, { theme: 'snow' });
    editor.root.innerHTML = value || '';
    editor.on('text-change', function () {
        dotNetHelper.invokeMethodAsync('OnHtmlChanged', editor.root.innerHTML);
    });
    window.quillEditors[id] = editor;
};

window.disposeHtmlEditor = function (id) {
    const editor = window.quillEditors ? window.quillEditors[id] : null;
    if (editor) {
        editor.off('text-change');
        delete window.quillEditors[id];
    }
};

window.updateStatsChart = function (data) {
    if (!window.Chart) return;
    const ctx = document.getElementById('statsChart');
    if (!ctx) return;
    if (window.statsChart) window.statsChart.destroy();
    window.statsChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: ['Commandes', 'Clients', 'CA'],
            datasets: [{
                data: [data.totalOrders, data.uniqueCustomers, data.totalRevenue],
                backgroundColor: ['#0d6efd', '#198754', '#fd7e14']
            }]
        },
        options: { responsive: true, plugins: { legend: { display: false } } }
    });
};