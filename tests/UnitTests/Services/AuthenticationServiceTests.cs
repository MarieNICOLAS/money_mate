using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Data.Context;
using MoneyMate.Models;
using MoneyMate.Services.Implementations;
using MoneyMate.Services.Interfaces;

namespace UnitTests.Services
{
    [TestClass]
    public class AuthenticationServiceTests
    {
        [TestMethod]
        public async Task RegisterAsync_WithExistingEmail_ReturnsFailure()
        {
            Mock<IMoneyMateDbContext> dbContextMock = new();
            Mock<ISessionManager> sessionManagerMock = new();

            dbContextMock.Setup(x => x.GetUserByEmail("user@test.local"))
                .Returns(new User
                {
                    Id = 1,
                    Email = "user@test.local",
                    PasswordHash = "hash"
                });

            AuthenticationService service = new(dbContextMock.Object, sessionManagerMock.Object);

            var result = await service.RegisterAsync("user@test.local", "Password1!");

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("AUTH_EMAIL_ALREADY_EXISTS", result.ErrorCode);
        }

        [TestMethod]
        public async Task RegisterAsync_WithValidInput_ReturnsSuccess()
        {
            Mock<IMoneyMateDbContext> dbContextMock = new();
            Mock<ISessionManager> sessionManagerMock = new();

            dbContextMock.Setup(x => x.GetUserByEmail("user@test.local"))
                .Returns((User?)null);

            dbContextMock.Setup(x => x.InsertUser(It.IsAny<User>()))
                .Callback<User>(user => user.Id = 42)
                .Returns(42);

            AuthenticationService service = new(dbContextMock.Object, sessionManagerMock.Object);

            var result = await service.RegisterAsync(" User@Test.Local ", "Password1!", "eur");

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(42, result.Data.Id);
            Assert.AreEqual("user@test.local", result.Data.Email);
            Assert.AreEqual("EUR", result.Data.Devise);
        }

        [TestMethod]
        public async Task LoginAsync_WithValidCredentials_StartsSession()
        {
            Mock<IMoneyMateDbContext> dbContextMock = new();
            Mock<ISessionManager> sessionManagerMock = new();

            User user = new()
            {
                Id = 7,
                Email = "user@test.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!", workFactor: 12),
                IsActive = true,
                Role = "User"
            };

            dbContextMock.Setup(x => x.GetUserByEmail("user@test.local"))
                .Returns(user);

            AuthenticationService service = new(dbContextMock.Object, sessionManagerMock.Object);

            var result = await service.LoginAsync("user@test.local", "Password1!", true);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);
            sessionManagerMock.Verify(x => x.StartSession(user, true), Times.Once);
        }

        [TestMethod]
        public async Task ChangePasswordAsync_WithSamePassword_ReturnsFailure()
        {
            Mock<IMoneyMateDbContext> dbContextMock = new();
            Mock<ISessionManager> sessionManagerMock = new();

            User user = new()
            {
                Id = 5,
                Email = "user@test.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!", workFactor: 12),
                IsActive = true
            };

            dbContextMock.Setup(x => x.GetUserById(5))
                .Returns(user);

            AuthenticationService service = new(dbContextMock.Object, sessionManagerMock.Object);

            var result = await service.ChangePasswordAsync(5, "Password1!", "Password1!");

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("AUTH_SAME_PASSWORD", result.ErrorCode);
        }

        [TestMethod]
        public void ValidatePasswordStrength_WithStrongPassword_ReturnsTrue()
        {
            Mock<IMoneyMateDbContext> dbContextMock = new();
            Mock<ISessionManager> sessionManagerMock = new();

            AuthenticationService service = new(dbContextMock.Object, sessionManagerMock.Object);

            bool result = service.ValidatePasswordStrength("Password1!");

            Assert.IsTrue(result);
        }
    }
}
