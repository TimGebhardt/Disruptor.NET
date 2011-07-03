/**
 * Callback interface to be implemented for processing {@link IEntry}s as they become available in the {@link RingBuffer}
 *
 * @param <T> IEntry implementation storing the data for sharing during exchange or parallel coordination of an event.
 */

namespace Disruptor
{
    public interface IBatchHandler<T> where T : Entry
    {
        /**
     * Called when a publisher has committed an {@link IEntry} to the {@link RingBuffer}
     *
     * @param entry committed to the {@link RingBuffer}
     * @throws Exception if the IBatchHandler would like the exception handled further up the chain.
     */
        void OnAvailable(T entry); //throws Exception;

        /**
     * Called after each batch of items has been have been processed before the next waitFor call on a {@link IConsumerBarrier}.
     * <p>
     * This can be taken as a hint to do flush type operations before waiting once again on the {@link IConsumerBarrier}.
     * The user should not expect any pattern or frequency to the batch size.
     *
     * @throws Exception if the IBatchHandler would like the exception handled further up the chain.
     */
        void OnEndOfBatch(); // throws Exception;

        /**
     * Called when processing of {@link IEntry}s is complete for clean up.
     */
        void OnCompletion();
    }
}