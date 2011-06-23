namespace Disruptor
{
    public interface IConsumer //: Runnable
    {
        // Get the sequence up to which this IConsumer has consumed {@link IEntry}s
        long GetSequence();

        //* Signal that this IConsumer should stop when it has finished consuming at the next clean break.
        void Halt();
    }
}