/**
 * No operation version of a {@link IConsumer} that simply tracks a {@link RingBuffer}.
 * This is useful in tests or for pre-filling a {@link RingBuffer} from a producer.
 */

namespace Disruptor
{
    public class NoOpConsumer : IConsumer
    {
        private readonly IRingBuffer _ringBuffer;

        //* Construct a {@link IConsumer} that simply tracks a {@link RingBuffer}.
        public NoOpConsumer(IRingBuffer ringBuffer)
        {
            _ringBuffer = ringBuffer;
        }
		
		public long Sequence { get { return _ringBuffer.Cursor; } }


        public void Halt()
        {
        }


        public void Run()
        {
        }
    }
}