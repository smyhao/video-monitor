namespace EzvizPlayer.Services
{
    public static class GridCalculator
    {
        public static (int rows, int cols) Calculate(int count)
        {
            return count switch
            {
                <= 1 => (1, 1),
                2 => (1, 2),
                <= 4 => (2, 2),
                <= 6 => (2, 3),
                <= 9 => (3, 3),
                _ => (4, 4)
            };
        }
    }
}
