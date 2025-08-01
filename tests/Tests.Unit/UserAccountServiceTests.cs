using Microsoft.Extensions.Options;
using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using Mailjet.Client.TransactionalEmails.Response;
using Moq;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;
using Dapper;

namespace Tests.Unit
{
    public class UserAccountServiceTests
    {
        [Fact]
        public async Task InviteUserAsync_CreatesUserAndToken()
        {
            using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();
            var factory = new FakeSqliteConnectionFactory(conn);

            await conn.ExecuteAsync("CREATE TABLE IF NOT EXISTS Setting (Key TEXT PRIMARY KEY, Value TEXT NOT NULL)");
            await conn.ExecuteAsync("CREATE TABLE IF NOT EXISTS EmailTemplate (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL UNIQUE, HtmlContent TEXT NOT NULL, TextContent TEXT)");
            await conn.ExecuteAsync("INSERT INTO Setting (Key, Value) VALUES ('ExpeditorEmail','exp@onigiri.com')");
            await conn.ExecuteAsync("INSERT INTO Setting (Key, Value) VALUES ('ExpeditorName','Exp')");
            await conn.ExecuteAsync("INSERT INTO Setting (Key, Value) VALUES ('InvitationSubject','Sujet')");
            await conn.ExecuteAsync("INSERT INTO Setting (Key, Value) VALUES ('InvitationIntro','Intro')");
            await conn.ExecuteAsync("INSERT INTO Setting (Key, Value) VALUES ('Signature','Signature')");
            await conn.ExecuteAsync("INSERT INTO EmailTemplate (Name, HtmlContent, TextContent) VALUES ('UserInvitation','<p>{{Intro}}</p><a href=\"{{Link}}\">{{Link}}</a><p>{{Signature}}</p>','{{Intro}} {{Link}} {{Signature}}')");

            var settingSvc = new SettingService(factory);
            var templateSvc = new EmailTemplateService(factory);
            var mailjet = new Mock<IMailjetClient>();
            mailjet.Setup(x => x.SendTransactionalEmailAsync(It.IsAny<TransactionalEmail>(), It.IsAny<bool>(), It.IsAny<bool>()))
                  .ReturnsAsync(new TransactionalEmailResponse { Messages = [new MessageResult { Status = "success" }] });
            var emailSvc = new EmailService(mailjet.Object, settingSvc, new ErrorModalService(), templateSvc);
            var service = new UserAccountService(factory, emailSvc, Options.Create(new MagicLinkConfig()));

            await service.InviteUserAsync("new@test.com", "New", "https://app.com");

            var count = await conn.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM User WHERE Email='new@test.com'");
            Assert.Equal(1, count);
            var tokenCount = await conn.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM MagicLinkToken WHERE UserId = (SELECT Id FROM User WHERE Email='new@test.com')");
            Assert.Equal(1, tokenCount);
        }

        [Fact]
        public async Task ResendInvitationAsync_CreatesTokenForExistingUser()
        {
            using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();
            var factory = new FakeSqliteConnectionFactory(conn);

            await conn.ExecuteAsync("CREATE TABLE IF NOT EXISTS Setting (Key TEXT PRIMARY KEY, Value TEXT NOT NULL)");
            await conn.ExecuteAsync("CREATE TABLE IF NOT EXISTS EmailTemplate (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL UNIQUE, HtmlContent TEXT NOT NULL, TextContent TEXT)");
            await conn.ExecuteAsync("INSERT INTO Setting (Key, Value) VALUES ('ExpeditorEmail','exp@onigiri.com')");
            await conn.ExecuteAsync("INSERT INTO Setting (Key, Value) VALUES ('ExpeditorName','Exp')");
            await conn.ExecuteAsync("INSERT INTO Setting (Key, Value) VALUES ('InvitationSubject','Sujet')");
            await conn.ExecuteAsync("INSERT INTO Setting (Key, Value) VALUES ('InvitationIntro','Intro')");
            await conn.ExecuteAsync("INSERT INTO Setting (Key, Value) VALUES ('Signature','Signature')");
            await conn.ExecuteAsync("INSERT INTO EmailTemplate (Name, HtmlContent, TextContent) VALUES ('UserInvitation','<p>{{Intro}}</p><a href=\"{{Link}}\">{{Link}}</a><p>{{Signature}}</p>','{{Intro}} {{Link}} {{Signature}}')");

            var settingSvc = new SettingService(factory);
            var templateSvc = new EmailTemplateService(factory);
            var mailjet = new Mock<IMailjetClient>();
            mailjet.Setup(x => x.SendTransactionalEmailAsync(It.IsAny<TransactionalEmail>(), It.IsAny<bool>(), It.IsAny<bool>()))
                  .ReturnsAsync(new TransactionalEmailResponse { Messages = [new MessageResult { Status = "success" }] });
            var emailSvc = new EmailService(mailjet.Object, settingSvc, new ErrorModalService(), templateSvc);
            var service = new UserAccountService(factory, emailSvc, Options.Create(new MagicLinkConfig()));

            await service.ResendInvitationAsync(2, "https://app.com");

            var tokenCount = await conn.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM MagicLinkToken WHERE UserId = 2");
            Assert.Equal(1, tokenCount);
        }
    }
}
