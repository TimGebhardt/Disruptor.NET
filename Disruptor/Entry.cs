

namespace Disruptor
{
	using Disruptor.MemoryLayout;

	/// <summary>
	/// Entries are the items exchanged via a RingBuffer.
	/// </summary>
    public abstract class Entry
    {
        private CacheLineStorageLong _sequence;
        
        /// <summary>
        /// Get the sequence number assigned to this item in the series.
        /// Or explicitly set the sequence number for this Entry and a CommitCallback for 
        /// indicating when the producer is finished with assigning data for exchange.
        /// </summary>
        public long Sequence 
        {
        	get { return _sequence.Data; }
        	set { _sequence.Data = value; }
        }
    }
}