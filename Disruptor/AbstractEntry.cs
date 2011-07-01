using System.Threading;
using Disruptor.MemoryLayout;

namespace Disruptor
{
    public abstract class AbstractEntry : IEntry
    {
    	private CacheLineStorageLong _sequence;
        public long Sequence 
        {
        	get { return _sequence.Data; }
        	set { _sequence.Data = value; }
        }
    }
}