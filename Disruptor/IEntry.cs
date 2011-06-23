namespace Disruptor
{
    public interface IEntry
    {
        long GetSequence();
        void SetSequence(long sequence);
    }
}