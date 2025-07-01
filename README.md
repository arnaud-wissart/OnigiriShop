# OnigiriShop

Problème technique, contre temps, le code arrivera sous peu.

# Configuration des secrets Mailjet

Pour le dev local, configurez vos clés Mailjet avec user-secrets :

dotnet user-secrets set "Mailjet:ApiKey" "XXX"
dotnet user-secrets set "Mailjet:ApiSecret" "XXX"
dotnet user-secrets set "Mailjet:SenderEmail" "XXX"
dotnet user-secrets set "Mailjet:SenderName" "XXX"
dotnet user-secrets set "Mailjet:AdminEmail" "XXX"
