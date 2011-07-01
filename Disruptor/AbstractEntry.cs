using System.Threading;

namespace Disruptor
{
    public abstract class AbstractEntry : IEntry
    {
    	private long _sequence;
        public long Sequence 
        {
        	get { return Thread.VolatileRead(ref _sequence); }
        	set { Thread.VolatileWrite(ref _sequence, value); }
        }
    }
}