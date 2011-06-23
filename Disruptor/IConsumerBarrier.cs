/**
 * Coordination barrier for tracking the cursor for producers and sequence of
 * dependent {@link Consumer}s for a {@link RingBuffer}
 *
 * @param <T> {@link IEntry} implementation stored in the {@link RingBuffer}
 */

namespace Disruptor
{
    public interface IConsumerBarrier
    {
        long WaitFor(long sequence);
        long WaitFor(long sequence, long timeout, TimeUnit units);
        long GetCursor();
        bool IsAlerted();
        void Alert();
        void ClearAlert();
    }

    public interface IConsumerBarrier<T> : IConsumerBarrier where T : IEntry
    {
        T GetEntry(long sequence);
    }
}