/**
 * Used by the {@link BatchConsumer} to set a callback allowing the {@link IBatchHandler} to notify
 * when it has finished consuming an {@link IEntry} if this happens after the {@link IBatchHandler#onAvailable(IEntry)} call.
 * <p>
 * Typically this would be used when the handler is performing some sort of batching operation such are writing to an IO device.
 * </p>
 * @param <T> IEntry implementation storing the data for sharing during exchange or parallel coordination of an event.
 */

namespace Disruptor
{
    public interface ISequenceTrackingHandler<T> : IBatchHandler<T> where T : IEntry
    {
        /**
     * Call by the {@link BatchConsumer} to setup the callback.
     *
     * @param sequenceTrackerCallback callback on which to notify the {@link BatchConsumer} that the sequence has progressed.
     */
        void SetSequenceTrackerCallback(BatchConsumer<T>.SequenceTrackerCallback sequenceTrackerCallback);
    }
}