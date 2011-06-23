/**
 * Called by the {@link RingBuffer} to pre-populate all the {@link IEntry}s to fill the RingBuffer.
 * 
 * @param <T> IEntry implementation storing the data for sharing during exchange or parallel coordination of an event.
 */

namespace Disruptor
{
    public interface IEntryFactory<T> where T : IEntry
    {
        T Create();
    }
}