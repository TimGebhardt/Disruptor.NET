namespace Disruptor
{
    public interface IRingBuffer
    {
        int Capacity { get; }
        long Cursor { get; }
    }

    public interface IRingBuffer<T> : IRingBuffer where T : IEntry
    {
        IConsumerBarrier<T> CreateConsumerBarrier(params IConsumer[] consumersToTrack);
        IProducerBarrier<T> CreateProducerBarrier(params IConsumer[] consumersToTrack);
        IForceFillProducerBarrier<T> CreateForceFillProducerBarrier(params IConsumer[] consumersToTrack);
        T GetEntry(long sequence);
    }
}