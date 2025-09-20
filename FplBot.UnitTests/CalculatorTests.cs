using FplBot.Utilities;

namespace FplBot.UnitTests
{
    public class Tests
    {
        [Theory]
        [TestCase(550, 15.38)]
        [TestCase(145, 40.82)]
        [TestCase(-255, 71.83)]
        public void CalculateWinPercentage_ShouldCalculateWinPercentage(int price, decimal percentage)
        {

            // Act
            var result = Calculator.CalculateWinPercentage(price);

            // Assert
            Assert.AreEqual(percentage, Math.Round(result, 2));
        }
    }
}