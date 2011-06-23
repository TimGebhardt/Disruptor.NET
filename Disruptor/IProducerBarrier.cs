/**
 * Abstraction for claiming {@link IEntry}s in a {@link RingBuffer} while tracking dependent {@link IConsumer}s
 *
 * @param <T> {@link IEntry} implementation stored in the {@link RingBuffer}
 */

namespace Disruptor
{
    public interface IProducerBarrier<T> where T : IEntry
    {
        T NextEntry();
        void Commit(T entry);

     // @return value of the cursor for entries that have been published.
        long GetCursor();
    }
}