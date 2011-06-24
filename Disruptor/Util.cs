using System.Collections.Generic;
using System.Linq;

namespace Disruptor
{
	///<summary>
	///Common set of functions used by Disruptor
	///</summary>
    public class Util
    {
        public static int CeilingNextPowerOfTwo(int x)
        {
			//This isn't the fastest way to calculate it, but it's clear
			//and it's probably not called a lot for a particular program
			
            int retval = 1;

            while (retval < x)
                retval *= 2;

            return retval;
        }

		///<summary>
		/// Get the minimum Sequence from a collection of IConsumers
		///</summary>
		///<param name="consumers">The consumers to compare</param>
		///<returns>The minimum sequence found, or long.MaxValue if the collection is empty</returns>
        public static long GetMinimumSequence(IEnumerable<IConsumer> consumers)
        {
			if(consumers.FirstOrDefault() == null)
				return long.MaxValue;
            return consumers.Min(consumer => consumer.Sequence);
        }
    }
}