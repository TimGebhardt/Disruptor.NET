/**
 * Abstraction for claiming {@link IEntry}s in a {@link RingBuffer} while tracking dependent {@link IConsumer}s
 *
 * @param <T> {@link IEntry} implementation stored in the {@link RingBuffer}
 */

namespace Disruptor
{
    public interface IProducerBarrier<T> where T : IEntry
    {
        /**
     * Claim the next {@link IEntry} in sequence for a producer on the {@link RingBuffer}
     *
     * @return the claimed {@link IEntry}
     */
        T NextEntry();

        /**
     * Commit an entry back to the {@link RingBuffer} to make it visible to {@link IConsumer}s
     * @param entry to be committed back to the {@link RingBuffer}
     */
        void Commit(T entry);

        /**
     * Delegate a call to the {@link RingBuffer#getCursor()}
     *
     * @return value of the cursor for entries that have been published.
     */
        long GetCursor();
    }
}