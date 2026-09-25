using FplBot.Utilities;

namespace FplBot.UnitTests;

public class PercentageTests
{
    [TestCase(0, 0)]
    [TestCase(1, 100)]
    [TestCase(0.71, 71)]
    [TestCase(0.674, 67)]
    [TestCase(0.675, 68)]
    [TestCase(0.625, 63)]
    public void ConvertsToWholePercentage(decimal probability, int expected) =>
        Assert.That(Percentage.FromProbability(probability), Is.EqualTo(expected));

    [Test]
    public void PreservesMissingValues() => Assert.That(Percentage.FromProbability(null), Is.Null);

    [TestCase(71.83, 72)]
    [TestCase(40.5, 41)]
    public void RoundsExistingPercentagesWithoutRescaling(decimal percentage, int expected) =>
        Assert.That(Percentage.Round(percentage), Is.EqualTo(expected));
}
