namespace Apex.Extensions
{
    public static class IntExtentions
    {
        public static int Clamp(this int Value, int Min, int Max)
        {
            if (Value < Min)
                return Min;
            if (Value > Max)
                return Max;
            return Value;
        }
    }
}
