namespace Disruptor
{
    public class Util
    {
        //public static int ceilingNextPowerOfTwo(int x)
        //{
        //    return 1 << (32 - Integer.numberOfLeadingZeros(x - 1));
        //}

        public static int CeilingNextPowerOfTwo(int x)
        {
            int value = 1;

            while (value < x)
                value *= 2;

            return value;
        }

        // Get the minimum sequence from an array of {@link IConsumer}s.
        // @param consumers to compare.
        //@return the minimum sequence found or Long.MAX_VALUE if the array is empty.
        public static long GetMinimumSequence(IConsumer[] consumers)
        {
            long min = long.MaxValue;

            for (int i = 0; i < consumers.Length; i++)
            {
                long sequence = consumers[i].Sequence;
                if (sequence < min)
                    min = sequence;
            }
            return min;
        }
    }
}