using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace Disruptor.Test
{
    [TestFixture]
    public class RingBufferTest
    {
        //private static  ExecutorService EXECUTOR = Executors.newSingleThreadExecutor(new DaemonThreadFactory());
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
        public void shouldClaimAndGet() // 
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

        //[Test]
        //public void shouldClaimAndGetInSeparateThread()
        //{
        //    getMessages(0, 0);

        //    StubEntry expectedEntry = new StubEntry(2701);

        //    StubEntry oldEntry = producerBarrier.NextEntry();
        //    oldEntry.copy(expectedEntry);
        //    producerBarrier.Commit(oldEntry);
        //    //Assert.IsTrue(messages.WaitUntilCompleted(1000));
        //    Assert.AreEqual(expectedEntry, _list[0].Value);
        //}

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

        private void getMessages(long initial, long toWaitFor)
            //   throws InterruptedException, BrokenBarrierException
        {
            //CyclicBarrier cyclicBarrier = new CyclicBarrier(2);
            IConsumerBarrier<StubEntry> consumerBarrier = ringBuffer.CreateConsumerBarrier();

            //    Future<List<StubEntry>> f = EXECUTOR.submit(new TestWaiter(cyclicBarrier, consumerBarrier, initial, toWaitFor));
            _list = new List<StubEntry>();
            var thread = new Thread((new TestWaiter(_list, consumerBarrier, initial, toWaitFor)).Call);
            thread.Start();
            Thread.Sleep(1000);
            thread.Join();
            //   cyclicBarrier.Await();
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

    public class TestWaiter //implements Callable<List<StubEntry>>
    {
        private readonly long toWaitForSequence;
        private readonly List<StubEntry> _entries;
        private readonly long initialSequence;
        // private  CyclicBarrier cyclicBarrier;
        private readonly IConsumerBarrier<StubEntry> consumerBarrier;
        private List<StubEntry> _stubEntries;

        public TestWaiter(List<StubEntry> entries,
                          IConsumerBarrier<StubEntry> consumerBarrier,
                          long initialSequence,
                          long toWaitForSequence)
        {
            // this.cyclicBarrier = cyclicBarrier;
            _entries = entries;
            this.initialSequence = initialSequence;
            this.toWaitForSequence = toWaitForSequence;
            this.consumerBarrier = consumerBarrier;
        }


        public void Call() // throws Exception
        {
            //  cyclicBarrier.await();
            Console.WriteLine("TestWaiter about to wait");
            consumerBarrier.WaitFor(toWaitForSequence);
            _entries.Clear();
            for (long l = initialSequence; l <= toWaitForSequence; l++)
            {
                _stubEntries.Add(consumerBarrier.GetEntry(l));
            }
            Console.WriteLine("TestWaiter finished");
        }
    }
}