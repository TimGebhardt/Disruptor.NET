namespace Disruptor
{
    public interface IConsumer
    {
		/// <summary>
		/// Get the sequence up to which this IConsumer has consumed {@link IEntry}s
		/// </summary>
		long Sequence { get; }
        // 

        /// <summary>
        /// Signal that this IConsumer should stop when it has finished consuming at the next clean break.
        /// </summary>
        void Halt();
    }
}