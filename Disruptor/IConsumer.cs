/**
 * EntryConsumers waitFor {@link IEntry}s to become available for consumption from the {@link RingBuffer}
 */

namespace Disruptor
{
    public interface IConsumer //: Runnable
    {
        /**
     * Get the sequence up to which this IConsumer has consumed {@link IEntry}s
     *
     * @return the sequence of the last consumed {@link IEntry}
     */
        long GetSequence();

        /**
     * Signal that this IConsumer should stop when it has finished consuming at the next clean break.
     * It will call {@link IConsumerBarrier#alert()} to notify the thread to check status.
     */
        void Halt();
    }
}