window.onigiriAuth = {
    login: async function(email, password) {
        try {
            const response = await fetch("/api/auth/login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                credentials: "same-origin"
            , body: JSON.stringify({ email, password }) });
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
window.onigiriAuth.logout = async function() {
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
window.hideAllTooltips = function () {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
        var tooltip = bootstrap.Tooltip.getInstance(el);
        if (tooltip) tooltip.hide();
    });
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
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(function (tooltipTriggerEl) {
        var tooltip = bootstrap.Tooltip.getOrCreateInstance(tooltipTriggerEl);
        tooltip.show(); // show puis hide pour forcer l’init
        tooltip.hide();
    });
};