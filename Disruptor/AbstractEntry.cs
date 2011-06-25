namespace Disruptor
{
    public abstract class AbstractEntry : IEntry
    {
        public long Sequence { get; set; }
    }
}