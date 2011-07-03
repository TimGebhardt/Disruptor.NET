/**
 * Abstraction for claiming {@link Entry}s in a {@link RingBuffer} while tracking dependent {@link Consumer}s.
 *
 * This barrier can be used to pre-fill a {@link RingBuffer} but only when no other producers are active.
 *
 * @param <T> {@link IEntry} implementation stored in the {@link RingBuffer}
 */

namespace Disruptor
{
    public interface IForceFillProducerBarrier<T> where T : Entry
    {
        /**
     * Claim a specific sequence in the {@link RingBuffer} when only one producer is involved.
     *
     * @param sequence to be claimed.
     * @return the claimed {@link IEntry}
     */
        T ClaimEntry(long sequence);

        /**
     * Commit an entry back to the {@link RingBuffer} to make it visible to {@link IConsumer}s.
     * Only use this method when forcing a sequence and you are sure only one producer exists.
     * This will cause the {@link RingBuffer} to advance the {@link RingBuffer#getCursor()} to this sequence.
     *
     * @param entry to be committed back to the {@link RingBuffer}
     */
        void Commit(T entry);

        /**
     * Delegate a call to the {@link RingBuffer#getCursor()}
     *
     * @return value of the cursor for entries that have been published.
     */
    long Cursor { get; }
    }
}