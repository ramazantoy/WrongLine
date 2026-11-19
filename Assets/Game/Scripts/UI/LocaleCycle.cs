namespace WrongLine.UI
{
    /// <summary>Which language the arrows pick: the neighbour in the list, wrapping around.</summary>
    public static class LocaleCycle
    {
        /// <summary>Index of the language <paramref name="step"/> places from <paramref name="current"/> (-1 for the previous, +1 for the next). An unknown current starts from the first.</summary>
        public static int Step(int current, int count, int step)
        {
            if (count <= 0)
            {
                return -1;
            }

            if (current < 0 || current >= count)
            {
                return 0;
            }

            return ((current + step) % count + count) % count;
        }
    }
}
