namespace FplBot.Utilities
{
    public static class Calculator
    {
        public static decimal CalculateWinPercentage(this int price)
        {
            if (price < 0)
            {
                int absPrice = Math.Abs(price);
                return Math.Round(((decimal)absPrice / ((decimal)absPrice + 100)) * 100, 2);
            }
            else if (price > 0)
            {
                return Math.Round((100 / ((decimal)price + 100)) * 100, 2);
            }
            return 0;
        }
    }
}
