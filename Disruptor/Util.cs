/**
 * Set of common functions used by the Disruptor
 */

using System.Collections.Generic;
using System.Linq;

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
			//TODO: Look at Java code to see if this matches Java output
            int value = 1;

            while (value < x)
                value *= 2;

            return value;
        }

        // Get the minimum sequence from an array of {@link IConsumer}s.
        // @param consumers to compare.
        //@return the minimum sequence found or Long.MAX_VALUE if the array is empty.
        public static long GetMinimumSequence(IEnumerable<IConsumer> consumers)
        {
			if(consumers.FirstOrDefault() == null)
				return long.MaxValue;
            return consumers.Min(consumer => consumer.Sequence);
        }
    }
}