// Called by the {@link RingBuffer} to pre-populate all the {@link IEntry}s to fill the RingBuffer.
namespace Disruptor
{
    public interface IEntryFactory<T> where T : IEntry
    {
        T Create();
    }
}