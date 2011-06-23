// Strategies employed for claiming the sequence of {@link IEntry}s in the {@link RingBuffer} by producers.
// The {@link IEntry} index is a the sequence value mod the {@link RingBuffer} capacity.

using System.Threading;

namespace Disruptor
{
    public interface IClaimStrategy
    {
        long GetAndIncrement();
        void SetSequence(long sequence);
        void WaitForCursor(long sequence, IRingBuffer ringBuffer);
    }

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