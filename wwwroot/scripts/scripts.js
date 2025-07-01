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