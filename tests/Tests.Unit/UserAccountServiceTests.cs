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

            await conn.ExecuteAsync("CREATE TABLE IF NOT EXISTS EmailVariation (Id INTEGER PRIMARY KEY AUTOINCREMENT, Type TEXT NOT NULL, Name TEXT, Value TEXT NOT NULL, Extra TEXT)");
            await conn.ExecuteAsync("CREATE TABLE IF NOT EXISTS EmailTemplate (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL UNIQUE, HtmlContent TEXT NOT NULL, TextContent TEXT)");
            await conn.ExecuteAsync("INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('Expeditor','def','exp@onigiri.com','Exp')");
            await conn.ExecuteAsync("INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('InvitationSubject',NULL,'Sujet',NULL)");
            await conn.ExecuteAsync("INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('InvitationIntro',NULL,'Intro',NULL)");
            await conn.ExecuteAsync("INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('Signature',NULL,'Signature',NULL)");
            await conn.ExecuteAsync("INSERT INTO EmailTemplate (Name, HtmlContent, TextContent) VALUES ('UserInvitation','<p>{{Intro}}</p><a href=\"{{Link}}\">{{Link}}</a><p>{{Signature}}</p>','{{Intro}} {{Link}} {{Signature}}')");

            var variationSvc = new EmailVariationService(factory);
            var templateSvc = new EmailTemplateService(factory);
            var mailjet = new Mock<IMailjetClient>();
            mailjet.Setup(x => x.SendTransactionalEmailAsync(It.IsAny<TransactionalEmail>(), It.IsAny<bool>(), It.IsAny<bool>()))
                  .ReturnsAsync(new TransactionalEmailResponse { Messages = [new MessageResult { Status = "success" }] });
            var emailSvc = new EmailService(mailjet.Object, variationSvc, new ErrorModalService(), templateSvc);
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

            await conn.ExecuteAsync("CREATE TABLE IF NOT EXISTS EmailVariation (Id INTEGER PRIMARY KEY AUTOINCREMENT, Type TEXT NOT NULL, Name TEXT, Value TEXT NOT NULL, Extra TEXT)");
            await conn.ExecuteAsync("CREATE TABLE IF NOT EXISTS EmailTemplate (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL UNIQUE, HtmlContent TEXT NOT NULL, TextContent TEXT)");
            await conn.ExecuteAsync("INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('Expeditor','def','exp@onigiri.com','Exp')");
            await conn.ExecuteAsync("INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('InvitationSubject',NULL,'Sujet',NULL)");
            await conn.ExecuteAsync("INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('InvitationIntro',NULL,'Intro',NULL)");
            await conn.ExecuteAsync("INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('Signature',NULL,'Signature',NULL)");
            await conn.ExecuteAsync("INSERT INTO EmailTemplate (Name, HtmlContent, TextContent) VALUES ('UserInvitation','<p>{{Intro}}</p><a href=\"{{Link}}\">{{Link}}</a><p>{{Signature}}</p>','{{Intro}} {{Link}} {{Signature}}')");

            var variationSvc = new EmailVariationService(factory);
            var templateSvc = new EmailTemplateService(factory);
            var mailjet = new Mock<IMailjetClient>();
            mailjet.Setup(x => x.SendTransactionalEmailAsync(It.IsAny<TransactionalEmail>(), It.IsAny<bool>(), It.IsAny<bool>()))
                  .ReturnsAsync(new TransactionalEmailResponse { Messages = [new MessageResult { Status = "success" }] });
            var emailSvc = new EmailService(mailjet.Object, variationSvc, new ErrorModalService(), templateSvc);
            var service = new UserAccountService(factory, emailSvc, Options.Create(new MagicLinkConfig()));

            await service.ResendInvitationAsync(2, "https://app.com");

            var tokenCount = await conn.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM MagicLinkToken WHERE UserId = 2");
            Assert.Equal(1, tokenCount);
        }
    }
}