namespace FplBot.Utilities;

public static class Percentage
{
    public static int Round(decimal percentage) => (int)Math.Round(percentage, MidpointRounding.AwayFromZero);

    public static int? FromProbability(decimal? probability) =>
        probability.HasValue ? Round(probability.Value * 100) : null;
}
