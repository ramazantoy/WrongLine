namespace WrongLine.Traffic
{
    /// <summary>
    /// What a traffic car tells its shader through the renderer's user value (one integer per renderer, in the per-draw data, so the
    /// SRP Batcher keeps batching): bits 0 to 3 the paint colour's index, bit 4 the left turn signal lit, bit 5 the right one.
    /// The shader unpacks it the same way (<c>CurvedLit</c>, <c>_TRAFFIC_ON</c>): change one, change the other.
    /// </summary>
    public static class TrafficRendererValue
    {
        public const int MaxPaints = 16;

        private const uint PaintMask = 0xFu;
        private const uint LeftSignalBit = 1u << 4;
        private const uint RightSignalBit = 1u << 5;

        public static uint Pack(int paint, bool leftSignal, bool rightSignal)
        {
            uint value = (uint)paint & PaintMask;
            if (leftSignal)
            {
                value |= LeftSignalBit;
            }

            if (rightSignal)
            {
                value |= RightSignalBit;
            }

            return value;
        }

        public static int Paint(uint value)
        {
            return (int)(value & PaintMask);
        }

        public static bool LeftSignal(uint value)
        {
            return (value & LeftSignalBit) != 0u;
        }

        public static bool RightSignal(uint value)
        {
            return (value & RightSignalBit) != 0u;
        }
    }
}
