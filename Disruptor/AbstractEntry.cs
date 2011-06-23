namespace Disruptor
{
    public abstract class AbstractEntry : IEntry
    {
        private long _sequence;

        public long GetSequence()
        {
            return _sequence;
        }

        public void SetSequence(long sequence)
        {
            _sequence = sequence;
        }
    }
}