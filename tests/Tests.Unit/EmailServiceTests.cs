using Dapper;
using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using Mailjet.Client.TransactionalEmails.Response;
using Moq;
using OnigiriShop.Services;

namespace Tests.Unit
{
    public class EmailServiceTests
    {
        [Fact]
        public async Task SendUserInvitationAsync_UsesTemplateAndVariations()
        {
            using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();
            await conn.ExecuteAsync("CREATE TABLE IF NOT EXISTS EmailVariation (Id INTEGER PRIMARY KEY AUTOINCREMENT, Type TEXT NOT NULL, Name TEXT, Value TEXT NOT NULL, Extra TEXT)");
            await conn.ExecuteAsync("CREATE TABLE IF NOT EXISTS EmailTemplate (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL UNIQUE, HtmlContent TEXT NOT NULL, TextContent TEXT)");
            await conn.ExecuteAsync("INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('Expeditor', 'def', 'exp@onigiri.com', 'Exp')");
            await conn.ExecuteAsync("INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('InvitationSubject', NULL, 'Sujet', NULL)");
            await conn.ExecuteAsync("INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('InvitationIntro', NULL, 'Intro', NULL)");
            await conn.ExecuteAsync("INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('Signature', NULL, 'Signature', NULL)");
            await conn.ExecuteAsync(@"INSERT INTO EmailTemplate (Name, HtmlContent, TextContent)
                VALUES ('UserInvitation','<p>{{Intro}}</p><a href=""{{Link}}"">{{Link}}</a><p>{{Signature}}</p>','{{Intro}} {{Link}} {{Signature}}')");

            var factory = new FakeSqliteConnectionFactory(conn);
            var variationSvc = new EmailVariationService(factory);
            var templateSvc = new EmailTemplateService(factory);

            var clientMock = new Mock<IMailjetClient>();
            TransactionalEmail? sent = null;
            clientMock.Setup(x => x.SendTransactionalEmailAsync(It.IsAny<TransactionalEmail>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(new TransactionalEmailResponse
                {
                    Messages = [new MessageResult { Status = "success" }]
                })
                .Callback<TransactionalEmail, bool, bool>((t, _, _) => sent = t);

            var service = new EmailService(clientMock.Object, variationSvc, new ErrorModalService(), templateSvc);
            var link = "https://example.com/invite";

            await service.SendUserInvitationAsync("john@doe.com", "John", link);

            clientMock.Verify(c => c.SendTransactionalEmailAsync(It.IsAny<TransactionalEmail>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            Assert.NotNull(sent);
            var html = (string?)typeof(TransactionalEmail).GetProperty("HTMLPart")!.GetValue(sent!) ?? string.Empty;
            var subject = (string?)typeof(TransactionalEmail).GetProperty("Subject")!.GetValue(sent!) ?? string.Empty;
            Assert.Contains("Intro", html);
            Assert.Contains(link, html);
            Assert.Contains("Signature", html);
            Assert.Equal("Sujet", subject);
        }
    }
}