// Strategies employed for claiming the sequence of {@link IEntry}s in the {@link RingBuffer} by producers.
// The {@link IEntry} index is a the sequence value mod the {@link RingBuffer} capacity.

using System.Threading;

namespace Disruptor
{
    public interface IClaimStrategy
    {
        // Claim the next sequence index in the {@link RingBuffer} and increment.
        //@return the {@link IEntry} index to be used for the producer.
        long GetAndIncrement();

        // Set the current sequence value for claiming {@link IEntry} in the {@link RingBuffer}
        // @param sequence to be set as the current value.
        void SetSequence(long sequence);

        // Wait for the current commit to reach a given sequence.
        // @param sequence to wait for.
        // @param ringBuffer on which to wait forCursor
        void WaitForCursor(long sequence, IRingBuffer ringBuffer);


        /**
     * Indicates the threading policy to be applied for claiming {@link IEntry}s by producers to the {@link com.lmax.disruptor.RingBuffer}
     */
    }


/**
     * Strategy to be used when there are multiple producer threads claiming {@link IEntry}s.
     */

    public class MultiThreadedStrategy : IClaimStrategy
    {
        private long _sequence;


        public long GetAndIncrement()
        {
            return Interlocked.Increment(ref _sequence);
        }


        public void SetSequence(long sequence)
        {
            Interlocked.Exchange(ref _sequence, sequence);
        }


        public void WaitForCursor(long sequence, IRingBuffer ringBuffer)
        {
            while (ringBuffer.GetCursor() != sequence)
            {
                // busy spin
            }
        }
    }

/**
     * Optimised strategy can be used when there is a single producer thread claiming {@link IEntry}s.
     */

    public class SingleThreadedStrategy : IClaimStrategy
    {
        private long _sequence;


        public long GetAndIncrement()
        {
            return _sequence++;
        }


        public void SetSequence(long sequence)
        {
            _sequence = sequence;
        }


        public void WaitForCursor(long sequence, IRingBuffer ringBuffer)
        {
            // no op for this class
        }
    }
}