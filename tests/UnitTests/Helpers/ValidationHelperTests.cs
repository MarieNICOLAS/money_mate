using Microsoft.VisualStudio.TestTools.UnitTesting;
using MoneyMate.Helpers;

namespace UnitTests.Helpers
{
    [TestClass]
    public class ValidationHelperTests
    {
        [TestMethod]
        public void IsValidEmail_WithValidEmail_ReturnsTrue()
        {
            bool result = ValidationHelper.IsValidEmail("user@test.local");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsValidEmail_WithInvalidEmail_ReturnsFalse()
        {
            bool result = ValidationHelper.IsValidEmail("user-test.local");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetPasswordStrength_WithEmptyPassword_ReturnsZero()
        {
            int result = ValidationHelper.GetPasswordStrength(string.Empty);

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void GetPasswordStrength_WithStrongPassword_ReturnsFour()
        {
            int result = ValidationHelper.GetPasswordStrength("Password1!");

            Assert.AreEqual(4, result);
        }

        [TestMethod]
        public void IsValidAmount_WithPositiveAmount_ReturnsTrue()
        {
            bool result = ValidationHelper.IsValidAmount(10.50m);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsValidAmount_WithZeroAmount_ReturnsFalse()
        {
            bool result = ValidationHelper.IsValidAmount(0m);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsValidDate_WithFutureDate_ReturnsFalse()
        {
            bool result = ValidationHelper.IsValidDate(DateTime.Now.AddMinutes(1));

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetPasswordStrengthLabel_WithScoreFour_ReturnsFort()
        {
            string result = ValidationHelper.GetPasswordStrengthLabel(4);

            Assert.AreEqual("Fort", result);
        }
    }
}
