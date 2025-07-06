using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using Microsoft.Extensions.Options;
using OnigiriShop.Infrastructure;
using Serilog;

namespace OnigiriShop.Services
{
    public class EmailService
    {
        private readonly IMailjetClient _client;
        private readonly MailjetConfig _mailjetConfig;
        public EmailService(IOptions<MailjetConfig> mailjetOptions)
        {
            _mailjetConfig = mailjetOptions.Value;
            _client = new MailjetClient(_mailjetConfig.ApiKey, _mailjetConfig.ApiSecret);            
        }
        public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlContent, string textContent = null)
        {
            try
            {
                var email = new TransactionalEmailBuilder()
                    .WithFrom(new SendContact(_mailjetConfig.SenderEmail, _mailjetConfig.SenderName))
                    .WithSubject(subject)
                    .WithHtmlPart(htmlContent)
                    .WithTo(new SendContact(toEmail, toName));

                if (!string.IsNullOrWhiteSpace(textContent))
                    email = email.WithTextPart(textContent);

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
                throw; // TODO relancer ou gérer ici
            }
        }

        public Task SendAdminNotificationAsync(string subject, string htmlContent, string textContent = null)
            => SendEmailAsync(_mailjetConfig.AdminEmail, AuthConstants.RoleAdmin, subject, htmlContent, textContent);

        // Utilitaire pour invitations, magic link, etc.
        public Task SendUserInvitationAsync(string toEmail, string toName, string invitationLink)
        {
            var subject = "Bienvenue sur OnigiriShop – Activez votre compte";
            var html = $"<p>Bonjour {(string.IsNullOrEmpty(toName) ? toEmail : toName)},<br>" +
                       $"Votre compte a été créé. <br>Merci de cliquer sur ce lien pour définir votre mot de passe :<br>" +
                       $"<a href=\"{invitationLink}\">{invitationLink}</a><br><br>" +
                       $"Ce lien expire dans 1 heure.</p>";
            var text = $"Bonjour {(string.IsNullOrEmpty(toName) ? toEmail : toName)},\n" +
                       $"Votre compte a été créé. Merci de cliquer sur ce lien pour définir votre mot de passe :\n" +
                       $"{invitationLink}\n\nCe lien expire dans 1 heure.";
            return SendEmailAsync(toEmail, toName, subject, html, text);
        }
    }

}
