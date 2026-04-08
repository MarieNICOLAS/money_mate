using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Models;
using MoneyMate.Services.Implementations;
using MoneyMate.Services.Interfaces;

namespace IntegrationTests.Services
{
    [TestClass]
    public class AuthenticationServiceIntegrationTests : TestDatabaseFixture
    {
        [TestMethod]
        public async Task RegisterAsync_WithValidInput_PersistsUser()
        {
            Mock<ISessionManager> sessionManagerMock = new();

            AuthenticationService service = new(DbContext, sessionManagerMock.Object);

            var result = await service.RegisterAsync("user@test.local", "Password1!", "eur");

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);

            User? persistedUser = DbContext.GetUserById(result.Data.Id);

            Assert.IsNotNull(persistedUser);
            Assert.AreEqual("user@test.local", persistedUser.Email);
            Assert.AreEqual("EUR", persistedUser.Devise);
            Assert.IsTrue(BCrypt.Net.BCrypt.Verify("Password1!", persistedUser.PasswordHash));
        }

        [TestMethod]
        public async Task LoginAsync_WithPersistedUser_ReturnsSuccess()
        {
            Mock<ISessionManager> sessionManagerMock = new();

            AuthenticationService service = new(DbContext, sessionManagerMock.Object);
            await service.RegisterAsync("login@test.local", "Password1!");

            var result = await service.LoginAsync("login@test.local", "Password1!", true);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);
            sessionManagerMock.Verify(x => x.StartSession(It.Is<User>(u => u.Email == "login@test.local"), true), Times.Once);
        }

        [TestMethod]
        public async Task ChangePasswordAsync_WithValidInput_UpdatesPasswordHash()
        {
            Mock<ISessionManager> sessionManagerMock = new();

            AuthenticationService service = new(DbContext, sessionManagerMock.Object);
            var registerResult = await service.RegisterAsync("changepwd@test.local", "Password1!");

            Assert.IsTrue(registerResult.IsSuccess);
            Assert.IsNotNull(registerResult.Data);

            var result = await service.ChangePasswordAsync(registerResult.Data.Id, "Password1!", "NewPassword1!");

            Assert.IsTrue(result.IsSuccess);

            User? persistedUser = DbContext.GetUserById(registerResult.Data.Id);

            Assert.IsNotNull(persistedUser);
            Assert.IsTrue(BCrypt.Net.BCrypt.Verify("NewPassword1!", persistedUser.PasswordHash));
        }
    }
}
