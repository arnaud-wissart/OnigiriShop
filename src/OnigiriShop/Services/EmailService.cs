using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using Serilog;

namespace OnigiriShop.Services
{
    public class EmailService
    {
        private readonly IMailjetClient _client;
        private readonly EmailVariationService _variationService;
        private readonly ErrorModalService _errorModalService;
        private readonly EmailTemplateService _templateService;

        public EmailService(IMailjetClient client, EmailVariationService variationService, ErrorModalService errorModalService, EmailTemplateService templateService)
        {
            _client = client;
            _variationService = variationService;
            _errorModalService = errorModalService;
            _templateService = templateService;
        }

        public async Task SendEmailAsync(
            string toEmail,
            string toName,
            string subject,
            string htmlContent,
            string textContent = null,
            string expEmail = null,
            string expName = null)
        {
            try
            {
                var fromContact = (!string.IsNullOrEmpty(expEmail) && !string.IsNullOrEmpty(expName))
                    ? new SendContact(expEmail, expName)
                    : new SendContact("no-reply@onigirishop.com", "OnigiriShop"); // Fallback

                var email = new TransactionalEmailBuilder()
                    .WithFrom(fromContact)
                    .WithSubject(subject)
                    .WithHtmlPart(htmlContent)
                    .WithTo(new SendContact(toEmail, toName));

                if (!string.IsNullOrWhiteSpace(textContent))
                    email = email.WithTextPart(textContent);

                email = email.WithCustomId(Guid.NewGuid().ToString());

                var response = await _client.SendTransactionalEmailAsync(email.Build());

                if (response == null || response.Messages == null || response.Messages.Length == 0)
                {
                    Log.Error("Aucune réponse Mailjet lors de l'envoi du mail à {ToEmail}", toEmail);
                    throw new Exception("Erreur lors de l'envoi du mail : aucune réponse de Mailjet.");
                }

                var msg = response.Messages[0];

                if (!string.Equals(msg.Status, "success", StringComparison.OrdinalIgnoreCase))
                {
                    var errors = msg.Errors != null ? string.Join(" / ", msg.Errors.Select(e => e.ErrorMessage)) : "Erreur inconnue";
                    Log.Error("Echec envoi mail à {ToEmail} : Statut={Status}, Erreurs={Errors}", toEmail, msg.Status, errors);
                    throw new Exception($"Mailjet ERROR : Statut = {msg.Status} - {errors}");
                }

                Log.Information("Email envoyé à {ToEmail} ({ToName}) : {Subject}", toEmail, toName, subject);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception lors de l'envoi du mail à {ToEmail} ({Subject})", toEmail, subject);
                _errorModalService .ShowModal(
                    "Une erreur est survenue lors de l'envoi de l'email : " + ex.Message,
                    "Erreur"
                );
            }
        }

        public async Task SendUserInvitationAsync(string toEmail, string toName, string invitationLink)
        {
            var (expEmail, expName) = await _variationService.GetRandomExpeditorAsync();
            var subject = await _variationService.GetRandomValueByTypeAsync("InvitationSubject") ?? "Bienvenue sur OnigiriShop";
            var intro = await _variationService.GetRandomValueByTypeAsync("InvitationIntro") ?? "Bienvenue !";
            var signature = await _variationService.GetRandomValueByTypeAsync("Signature") ?? "L’équipe OnigiriShop";

            var template = await _templateService.GetByNameAsync("UserInvitation");
            string html;
            string text;
            if (template != null)
            {
                html = template.HtmlContent
                    .Replace("{{Intro}}", intro)
                    .Replace("{{Link}}", invitationLink)
                    .Replace("{{Signature}}", signature);
                text = template.TextContent?
                    .Replace("{{Intro}}", intro)
                    .Replace("{{Link}}", invitationLink)
                    .Replace("{{Signature}}", signature);
            }
            else
            {
                html = $@"<p>{intro}</p><p>Ton compte a été créé. Clique ci-dessous pour définir ton mot de passe&nbsp;:</p><p><a href=""{invitationLink}"">{invitationLink}</a></p><p><small>Ce lien expire dans 1 heure.</small></p><hr><p style=""color:#888;font-size:0.9em;"">{signature}</p>";
                text = $"{intro}\nTon compte a été créé.\n{invitationLink}\nCe lien expire dans 1 heure.\n\n{signature}";
            }

            await SendEmailAsync(toEmail, toName, subject, html, text, expEmail, expName);
        }

        public async Task SendPasswordResetAsync(string toEmail, string toName, string resetLink)
        {
            var (expEmail, expName) = await _variationService.GetRandomExpeditorAsync();
            var subject = await _variationService.GetRandomValueByTypeAsync("PasswordResetSubject") ?? "Réinitialisation de votre mot de passe";
            var intro = await _variationService.GetRandomValueByTypeAsync("PasswordResetIntro") ?? "Vous avez demandé à réinitialiser votre mot de passe.";
            var signature = await _variationService.GetRandomValueByTypeAsync("Signature") ?? "L’équipe OnigiriShop";

            var template = await _templateService.GetByNameAsync("PasswordReset");
            string html;
            string text;
            if (template != null)
            {
                html = template.HtmlContent
                    .Replace("{{Intro}}", intro)
                    .Replace("{{Link}}", resetLink)
                    .Replace("{{Signature}}", signature);
                text = template.TextContent?
                    .Replace("{{Intro}}", intro)
                    .Replace("{{Link}}", resetLink)
                    .Replace("{{Signature}}", signature);
            }
            else
            {
                html = $@"<p>{intro}</p><p>Cliquez ici pour choisir un nouveau mot de passe&nbsp;:<br><a href=""{resetLink}"">{resetLink}</a></p><p><small>Ce lien est valable 1 heure.</small></p><hr><p style=""color:#888;font-size:0.9em;"">{signature}</p>";
                text = $"{intro}\n{resetLink}\nCe lien est valable 1 heure.\n\n{signature}";
            }

            await SendEmailAsync(toEmail, toName, subject, html, text, expEmail, expName);
        }

        public async Task SendOrderConfirmationAsync(string toEmail, string toName, Order order, Delivery delivery)
        {
            var (expEmail, expName) = await _variationService.GetRandomExpeditorAsync();
            var subject = await _variationService.GetRandomValueByTypeAsync("OrderSubject") ?? $"Commande n°{order.Id}";
            var signature = await _variationService.GetRandomValueByTypeAsync("Signature") ?? "L’équipe OnigiriShop";

            var orderLines = order.Items != null && order.Items.Any()
                ? string.Join("", order.Items.Select(i =>
                    $"<li>{i.Quantity} x {i.ProductName} - {(i.UnitPrice * i.Quantity):0.00} €</li>"))
                : "<li>Pas de détail trouvé.</li>";

            var template = await _templateService.GetByNameAsync("OrderConfirmation");
            string html;
            string text;
            if (template != null)
            {
                html = template.HtmlContent
                    .Replace("{{Name}}", string.IsNullOrEmpty(toName) ? toEmail : toName)
                    .Replace("{{OrderId}}", order.Id.ToString())
                    .Replace("{{OrderDate}}", order.OrderedAt.ToString("dd/MM/yyyy à HH:mm"))
                    .Replace("{{OrderLines}}", orderLines)
                    .Replace("{{Total}}", order.TotalAmount.ToString("0.00"))
                    .Replace("{{DeliveryDate}}", delivery.DeliveryAt.ToString("dddd dd/MM/yyyy HH:mm"))
                    .Replace("{{DeliveryPlace}}", delivery.Place)
                    .Replace("{{Signature}}", signature);
                text = template.TextContent?
                    .Replace("{{Name}}", string.IsNullOrEmpty(toName) ? toEmail : toName)
                    .Replace("{{OrderId}}", order.Id.ToString())
                    .Replace("{{OrderDate}}", order.OrderedAt.ToString("dd/MM/yyyy à HH:mm"))
                    .Replace("{{Total}}", order.TotalAmount.ToString("0.00"))
                    .Replace("{{DeliveryDate}}", delivery.DeliveryAt.ToString("dddd dd/MM/yyyy HH:mm"))
                    .Replace("{{DeliveryPlace}}", delivery.Place)
                    .Replace("{{Signature}}", signature)
                    .Replace("{{OrderLines}}", string.Join("\n", order.Items?.Select(i => $"{i.Quantity} x {i.ProductName} - {(i.UnitPrice * i.Quantity):0.00} €") ?? Enumerable.Empty<string>()));
            }
            else
            {
                html = $@"<p>Bonjour {(string.IsNullOrEmpty(toName) ? toEmail : toName)},</p><p>Nous avons bien reçu votre commande n°{order.Id} du {order.OrderedAt:dd/MM/yyyy à HH:mm}.</p><ul style=""margin-bottom:1em;"">{orderLines}</ul><p><b>Total : {order.TotalAmount:0.00} €</b></p><p>Livraison prévue : {delivery.DeliveryAt:dddd dd/MM/yyyy HH:mm} - {delivery.Place}</p><hr><p style=""color:#888;font-size:0.9em;"">{signature}</p>";
                text = $"Bonjour {(string.IsNullOrEmpty(toName) ? toEmail : toName)},\n" +
                       $"Commande n°{order.Id} du {order.OrderedAt:dd/MM/yyyy à HH:mm}\n" +
                       $"Total : {order.TotalAmount:0.00} €\n" +
                       $"Livraison prévue : {delivery.DeliveryAt:dddd dd/MM/yyyy HH:mm} - {delivery.Place}\n" +
                       $"Détail :\n" +
                       (order.Items != null
                           ? string.Join("\n", order.Items.Select(i => $"{i.Quantity} x {i.ProductName} - {(i.UnitPrice * i.Quantity):0.00} €"))
                           : "Pas de détail trouvé.") +
                       $"\n\n{signature}";

                await SendEmailAsync(toEmail, toName, subject, html, text, expEmail, expName);
            }
        }

        public async Task SendAdminNotificationAsync(string subject, string htmlContent, string textContent = null)
        {
            var (expEmail, expName) = await _variationService.GetRandomExpeditorAsync();
            // Suppose que tu stockes l'email admin dans les variations ou ailleurs
            var adminEmail = "admin@onigirishop.com";
            await SendEmailAsync(adminEmail, "Admin", subject, htmlContent, textContent, expEmail, expName);
        }
    }
}
