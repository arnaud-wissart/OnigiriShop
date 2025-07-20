using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Moq;
using OnigiriShop.Data;
using OnigiriShop.Data.Interfaces;
using OnigiriShop.Services;
using System.Security.Claims;

namespace Tests.Unit
{
    public class AuthServiceTests
    {
        private static AuthService BuildService(ClaimsPrincipal user)
        {
            var mockProvider = new Mock<SessionAuthenticationStateProvider>(null);
            mockProvider.Setup(x => x.GetAuthenticationStateAsync())
                .ReturnsAsync(new AuthenticationState(user));

            var cartService = new Mock<CartService>(MockBehavior.Loose, (ISqliteConnectionFactory)null);
            var anonCart = new Mock<AnonymousCartService>(MockBehavior.Loose, (IJSRuntime)null);
            var prodService = new Mock<ProductService>(MockBehavior.Loose, (ISqliteConnectionFactory)null);
            var authProvider = new Mock<AuthenticationStateProvider>();
            var cartProvider = new Mock<CartProvider>(cartService.Object, anonCart.Object, prodService.Object, authProvider.Object);

            return new AuthService(mockProvider.Object, cartProvider.Object);
        }

        [Fact]
        public async Task GetCurrentUserIdAsync_Returns_CorrectId()
        {
            // Arrange
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "42") };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            var service = BuildService(principal);

            // Act
            var result = await service.GetCurrentUserIdAsync();

            // Assert
            Assert.Equal("42", result);
        }

        [Fact]
        public async Task GetCurrentUserEmailAsync_Returns_Email()
        {
            var claims = new[] { new Claim(ClaimTypes.Email, "john@doe.com") };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            var service = BuildService(principal);

            var result = await service.GetCurrentUserEmailAsync();

            Assert.Equal("john@doe.com", result);
        }

        [Fact]
        public async Task GetCurrentUserNameOrEmailAsync_Returns_Email()
        {
            var claims = new[] { 
                new Claim(ClaimTypes.Email, "john@doe.com"),
                new Claim(ClaimTypes.Name, "Toto")
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            var service = BuildService(principal);

            var result = await service.GetCurrentUserNameOrEmailAsync();

            Assert.Equal("Toto", result);
        }

        [Fact]
        public async Task GetCurrentUserRoleAsync_DefaultsToUser_IfMissing()
        {
            var claims = Array.Empty<Claim>();
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            var service = BuildService(principal);

            var result = await service.GetCurrentUserRoleAsync();

            Assert.Equal("User", result); // Vérifie que la constante correspond à AuthConstants.RoleUser
        }

        [Fact]
        public async Task IsAdminAsync_Returns_True_IfAdmin()
        {
            var claims = new[] { new Claim(ClaimTypes.Role, "Admin") };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            var service = BuildService(principal);

            Assert.True(await service.IsAdminAsync());
        }

        [Fact]
        public async Task IsAuthenticatedAsync_Returns_True_IfAuthenticated()
        {
            var identity = new ClaimsIdentity("TestAuth");
            var principal = new ClaimsPrincipal(identity);
            identity.AddClaim(new Claim(ClaimTypes.Name, "Bob"));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "1"));

            var service = BuildService(principal);

            Assert.True(await service.IsAuthenticatedAsync());
        }

        [Fact]
        public async Task LogoutAsync_CallsProviderSignOut()
        {
            var mockProvider = new Mock<SessionAuthenticationStateProvider>(null);
            mockProvider.Setup(x => x.GetAuthenticationStateAsync())
                .ReturnsAsync(new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(
                    new ClaimsPrincipal(new ClaimsIdentity())));
            mockProvider.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask).Verifiable();

            var cartService = new Mock<CartService>(MockBehavior.Loose, (ISqliteConnectionFactory)null);
            var anonCart = new Mock<AnonymousCartService>(MockBehavior.Loose, (IJSRuntime)null);
            var prodService = new Mock<ProductService>(MockBehavior.Loose, (ISqliteConnectionFactory)null);
            var authProvider = new Mock<AuthenticationStateProvider>();
            var cartProvider = new Mock<CartProvider>(cartService.Object, anonCart.Object, prodService.Object, authProvider.Object);

            var service = new AuthService(mockProvider.Object, cartProvider.Object);

            await service.LogoutAsync();

            mockProvider.Verify(x => x.SignOutAsync(), Times.Once);
        }
    }
}
