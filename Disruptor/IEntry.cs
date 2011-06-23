/**
 * Entries are the items exchanged via a RingBuffer.
 */

namespace Disruptor
{
    public interface IEntry
    {
        long GetSequence();
        void SetSequence(long sequence);
    }
}