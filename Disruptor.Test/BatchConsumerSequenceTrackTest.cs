using System.Threading;
using NUnit.Framework;

namespace Disruptor.Test
{
    [TestFixture]
    public class BatchConsumerSequenceTrackingCallbackTest
    {
        private static readonly AutoResetEvent onAvailableLatch = new AutoResetEvent(false);
        private static readonly AutoResetEvent readyToCallbackLatch = new AutoResetEvent(false);

        [Test]
        public void ShouldReportProgressByUpdatingSequenceViaCallback()
        {
            IRingBuffer<StubEntry> ringBuffer = new RingBuffer<StubEntry>(new StubFactory(), 16);
            IConsumerBarrier<StubEntry> consumerBarrier = ringBuffer.CreateConsumerBarrier();
            ISequenceTrackingHandler<StubEntry> handler = new TestSequenceTrackingHandler();
            var batchConsumer = new BatchConsumer<StubEntry>(consumerBarrier, handler);
            IProducerBarrier<StubEntry> producerBarrier = ringBuffer.CreateProducerBarrier(batchConsumer);

            var thread = new Thread(batchConsumer.Run);
            thread.Start();

            Assert.AreEqual(-1L, batchConsumer.Sequence);
            producerBarrier.Commit(producerBarrier.NextEntry());
            producerBarrier.Commit(producerBarrier.NextEntry());
            Assert.IsTrue(onAvailableLatch.WaitOne(1000));
            Assert.AreEqual(-1L, batchConsumer.Sequence);

            producerBarrier.Commit(producerBarrier.NextEntry());
            Assert.IsTrue(readyToCallbackLatch.WaitOne(1000));
            Assert.AreEqual(2L, batchConsumer.Sequence);

            batchConsumer.Halt();
            thread.Join();
        }

        private class TestSequenceTrackingHandler : ISequenceTrackingHandler<StubEntry>
        {
            private BatchConsumer<StubEntry>.SequenceTrackerCallback sequenceTrackerCallback;


            public void SetSequenceTrackerCallback(
                BatchConsumer<StubEntry>.SequenceTrackerCallback sequenceTrackerCallback)
            {
                this.sequenceTrackerCallback = sequenceTrackerCallback;
            }


            public void OnAvailable(StubEntry entry) 
            {
                if (entry.Sequence == 2L)
                {
                    sequenceTrackerCallback.OnCompleted(entry.Sequence);
                    readyToCallbackLatch.Set();
                }
                else
                {
                    onAvailableLatch.Set();
                }
            }


            public void OnEndOfBatch()
            {
            }


            public void OnCompletion()
            {
            }
        }
    }
}