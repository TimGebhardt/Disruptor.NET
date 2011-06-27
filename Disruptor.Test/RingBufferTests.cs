using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Disruptor.Test
{
    [TestFixture]
    public class RingBufferTest
    {
        private RingBuffer<StubEntry> ringBuffer;
        private IConsumerBarrier<StubEntry> consumerBarrier;
        private IProducerBarrier<StubEntry> producerBarrier;
        private List<StubEntry> _list;

        [SetUp]
        public void setUp()
        {
            ringBuffer = new RingBuffer<StubEntry>(new StubFactory(), 20, new SingleThreadedStrategy(),
                                                   new BusySpinStrategy<StubEntry>());
            consumerBarrier = ringBuffer.CreateConsumerBarrier();
            producerBarrier = ringBuffer.CreateProducerBarrier(new NoOpConsumer(ringBuffer));
        }

        [Test]
        public void shouldClaimAndGet()
        {
            Assert.AreEqual(-1L, ringBuffer.Cursor);

            var expectedEntry = new StubEntry(2701);

            StubEntry oldEntry = producerBarrier.NextEntry();
            oldEntry.copy(expectedEntry);
            producerBarrier.Commit(oldEntry);

            long sequence = consumerBarrier.WaitFor(0);
            Assert.AreEqual(0, sequence);

            StubEntry entry = ringBuffer.GetEntry(sequence);
            Assert.AreEqual(expectedEntry, entry);

            Assert.AreEqual(0L, ringBuffer.Cursor);
        }

        [Test]
        public void shouldClaimAndGetWithTimeout() // 
        {
            Assert.AreEqual(-1L, ringBuffer.Cursor);

            var expectedEntry = new StubEntry(2701);

            StubEntry oldEntry = producerBarrier.NextEntry();
            oldEntry.copy(expectedEntry);
            producerBarrier.Commit(oldEntry);

            long sequence = consumerBarrier.WaitFor(0, 5);
            Assert.AreEqual(0, sequence);

            StubEntry entry = ringBuffer.GetEntry(sequence);
            Assert.AreEqual(expectedEntry, entry);

            Assert.AreEqual(0L, ringBuffer.Cursor);
        }


        [Test]
        public void shouldGetWithTimeout()
        {
            long sequence = consumerBarrier.WaitFor(0, 5);
            Assert.AreEqual(-1L, sequence);
        }

		[Test]
        public void shouldClaimAndGetInSeparateThread()
        {
            Task<List<StubEntry>> messages = GetMessages(0, 0);

	        StubEntry expectedEntry = new StubEntry(2701);
	
	        StubEntry oldEntry = producerBarrier.NextEntry();
	        oldEntry.copy(expectedEntry);
	        producerBarrier.Commit(oldEntry);
	
	        Assert.AreEqual(expectedEntry, messages.Result[0]);
        }

        [Test]
        public void shouldClaimAndGetMultipleMessages()
        {
            int numMessages = ringBuffer.Capacity;
            for (int i = 0; i < numMessages; i++)
            {
                StubEntry entry = producerBarrier.NextEntry();
                entry.Value = i;
                producerBarrier.Commit(entry);
            }

            int expectedSequence = numMessages - 1;
            long available = consumerBarrier.WaitFor(expectedSequence);
            Assert.AreEqual(expectedSequence, available);

            for (int i = 0; i < numMessages; i++)
            {
                Assert.AreEqual(i, ringBuffer.GetEntry(i).Sequence);
            }
        }

        [Test]
        public void shouldWrap()
        {
            int numMessages = ringBuffer.Capacity;
            int offset = 1000;
            for (int i = 0; i < numMessages + offset; i++)
            {
                StubEntry entry = producerBarrier.NextEntry();
                entry.Value = i;
                producerBarrier.Commit(entry);
            }

            int expectedSequence = numMessages + offset - 1;
            long available = consumerBarrier.WaitFor(expectedSequence);
            Assert.AreEqual(expectedSequence, available);

            for (int i = offset; i < numMessages + offset; i++)
            {
                Assert.AreEqual(i, ringBuffer.GetEntry(i).Sequence);
            }
        }

        [Test]
        public void shouldSetAtSpecificSequence()
        {
            long expectedSequence = 5;
            IForceFillProducerBarrier<StubEntry> forceFillProducerBarrier =
                ringBuffer.CreateForceFillProducerBarrier(new NoOpConsumer(ringBuffer));

            StubEntry expectedEntry = forceFillProducerBarrier.ClaimEntry(expectedSequence);
            expectedEntry.Value = (int) expectedSequence;
            forceFillProducerBarrier.Commit(expectedEntry);

            long sequence = consumerBarrier.WaitFor(expectedSequence);
            Assert.AreEqual(expectedSequence, sequence);

            StubEntry entry = ringBuffer.GetEntry(sequence);
            Assert.AreEqual(expectedEntry, entry);

            Assert.AreEqual(expectedSequence, ringBuffer.Cursor);
        }

	    private Task<List<StubEntry>> GetMessages(long initial, long toWaitFor)
	    {
	        var barrier = new AutoResetEvent(false);
	        var consumerBarrier = ringBuffer.CreateConsumerBarrier();
	
	        Task<List<StubEntry>> f = new Task<List<StubEntry>>(
				new TestWaiter(barrier, consumerBarrier, initial, toWaitFor).Call);
			f.Start();
	
	        barrier.WaitOne(TimeSpan.FromSeconds(10));
	
	        return f;
	    }
    }

    public class StubEntry : AbstractEntry
    {
        public int Value { get; set; }
        public string teststring { get; set; }

        public StubEntry(int i)
        {
            Value = i;
        }

        public void copy(StubEntry entry)
        {
            Value = entry.Value;
        }


        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime*result + Value;
            return result;
        }


        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj == null) return false;
            var other = (StubEntry) obj;

            return Value == other.Value;
        }
    }

    internal class StubFactory : IEntryFactory<StubEntry>
    {
        public StubEntry Create()
        {
            return new StubEntry(-1);
        }
    }

    public class TestWaiter
    {
        private readonly long toWaitForSequence;
        private readonly long initialSequence;
        private AutoResetEvent barrier;
        private readonly IConsumerBarrier<StubEntry> consumerBarrier;

        public TestWaiter(AutoResetEvent barrier,
                          IConsumerBarrier<StubEntry> consumerBarrier,
                          long initialSequence,
                          long toWaitForSequence)
        {
            this.barrier = barrier;
            this.initialSequence = initialSequence;
            this.toWaitForSequence = toWaitForSequence;
            this.consumerBarrier = consumerBarrier;
        }


        public List<StubEntry> Call()
        {
            barrier.Set();
            consumerBarrier.WaitFor(toWaitForSequence);
			
			List<StubEntry> retval = new List<StubEntry>();
            for (long l = initialSequence; l <= toWaitForSequence; l++)
            {
                retval.Add(consumerBarrier.GetEntry(l));
            }
			return retval;
        }
    }
}