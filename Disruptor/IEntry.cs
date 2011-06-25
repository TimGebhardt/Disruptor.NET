namespace Disruptor
{
    public interface IEntry
    {
        long Sequence { get; set; }
    }
}