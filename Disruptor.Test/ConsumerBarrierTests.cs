using System;
using System.Threading;
using NUnit.Framework;
using Rhino.Mocks;

namespace Disruptor.Test
{
    [TestFixture]
    public class ConsumerBarrierTest
    {
    	private MockRepository _mocks;
        private RingBuffer<StubEntry> ringBuffer;
        private IConsumer consumer1;
        private IConsumer consumer2;
        private IConsumer consumer3;
        private IConsumerBarrier<StubEntry> consumerBarrier;
        private IProducerBarrier<StubEntry> producerBarrier;

        [SetUp]
        public void setUp()
        {
        	_mocks = new MockRepository();

            ringBuffer = new RingBuffer<StubEntry>(new StubFactory(), 64);

            consumer1 = _mocks.DynamicMock<IConsumer>();
            consumer2 = _mocks.DynamicMock<IConsumer>();
            consumer3 = _mocks.DynamicMock<IConsumer>();

            consumerBarrier = ringBuffer.CreateConsumerBarrier(consumer1, consumer2, consumer3);
            producerBarrier = ringBuffer.CreateProducerBarrier(new NoOpConsumer(ringBuffer));
        }

		[Test]
		public void shouldWaitForWorkCompleteWhereCompleteWorkThresholdIsAhead()
		{
		    long expectedNumberMessages = 10;
		    long expectedWorkSequence = 9;
		    fillRingBuffer(expectedNumberMessages);
		    
		    Expect.Call(consumer1.Sequence).Return(expectedNumberMessages);
		    Expect.Call(consumer2.Sequence).Return(expectedWorkSequence);
		    Expect.Call(consumer3.Sequence).Return(expectedWorkSequence);
		    
		    _mocks.ReplayAll();
		    
			long completedWorkSequence = consumerBarrier.WaitFor(expectedWorkSequence);
			Assert.IsTrue(completedWorkSequence >= expectedWorkSequence);
			
			_mocks.VerifyAll();
		}

/*        [Test]
        public void shouldWaitForWorkCompleteWhereAllWorkersAreBlockedOnRingBuffer() // throws Exception
        {
            long expectedNumberMessages = 10;
            fillRingBuffer(expectedNumberMessages);

            var workers = new StubConsumer[3];
            for (int i = 0, size = workers.Length; i < size; i++)
            {
                workers[i] = new StubConsumer(0);
                workers[i].Sequence = (expectedNumberMessages - 1);
            }

            IConsumerBarrier consumerBarrier = ringBuffer.CreateConsumerBarrier(workers);
            ThreadStart runnable = () =>
                                       {
                                           StubEntry entry = producerBarrier.NextEntry();
                                           entry.Value = ((int) entry.Sequence);
                                           producerBarrier.Commit(entry);

                                           foreach (StubConsumer stubWorker in workers)
                                           {
                                               stubWorker.Sequence = (entry.Sequence);
                                           }
                                       };


            new Thread(runnable).Start();

            long expectedWorkSequence = expectedNumberMessages;
            long completedWorkSequence = consumerBarrier.WaitFor(expectedNumberMessages);
            Assert.IsTrue(completedWorkSequence >= expectedWorkSequence);
        }
*/
/*
        [Test]
        public void shouldInterruptDuringBusySpin() //throws Exception
        {
            long expectedNumberMessages = 10;
            fillRingBuffer(expectedNumberMessages);
            //     CountDownLatch latch = new CountDownLatch(9);

            //context.checking(new Expectations()
            //{
            //    {
            //        allowing(consumer1).getSequence();
            //        will(new DoAllAction(countDown(latch), returnValue(Long.valueOf(8L))));

            //        allowing(consumer2).getSequence();
            //        will(new DoAllAction(countDown(latch), returnValue(Long.valueOf(8L))));

            //        allowing(consumer3).getSequence();
            //        will(new DoAllAction(countDown(latch), returnValue(Long.valueOf(8L))));
            //    }
            //});

            bool[] Alerted = {false};
            ThreadStart runnable = () =>
                                       {
                                           try
                                           {
                                               consumerBarrier.WaitFor(expectedNumberMessages - 1);
                                           }
                                           catch (AlertException)
                                           {
                                               Alerted[0] = true;
                                           }
                                           catch (Exception)
                                           {
                                               // don't care
                                           }
                                       };

            var t = new Thread(runnable);

            t.Start();
            //  Assert.IsTrue(latch.Await(1, TimeUnit.SECONDS));
            Thread.Sleep(1000);
            consumerBarrier.Alert();
            t.Join();

            Assert.IsTrue(Alerted[0]);
        }
*/
/*
        [Test]
        public void shouldWaitForWorkCompleteWhereCompleteWorkThresholdIsBehind() //throws Exception
        {
            long expectedNumberMessages = 10;
            fillRingBuffer(expectedNumberMessages);

            var entryConsumers = new StubConsumer[3];
            for (int i = 0, size = entryConsumers.Length; i < size; i++)
            {
                entryConsumers[i] = new StubConsumer(1);
                entryConsumers[i].Sequence = (expectedNumberMessages - 2);
            }

            IConsumerBarrier consumerBarrier = ringBuffer.CreateConsumerBarrier(entryConsumers);

            ThreadStart runnable = () =>
                                       {
                                           foreach (StubConsumer stubWorker in entryConsumers)
                                           {
                                               stubWorker.Sequence += 1;
                                           }
                                       };


            new Thread(runnable).Start();

            long expectedWorkSequence = expectedNumberMessages - 1;
            long completedWorkSequence = consumerBarrier.WaitFor(expectedWorkSequence);
            Assert.IsTrue(completedWorkSequence >= expectedWorkSequence);
        }
*/
/*
        [Test]
        public void shouldSetAndClearAlertStatus()
        {
            Assert.IsFalse(consumerBarrier.IsAlerted());

            consumerBarrier.Alert();
            Assert.IsTrue(consumerBarrier.IsAlerted());

            consumerBarrier.ClearAlert();
            Assert.IsFalse(consumerBarrier.IsAlerted());
        }
*/

        private void fillRingBuffer(long expectedNumberMessages) // throws InterruptedException
        {
            for (long i = 0; i < expectedNumberMessages; i++)
            {
                StubEntry entry = producerBarrier.NextEntry();
                entry.Value = (int) i;
                producerBarrier.Commit(entry);
            }
        }

        internal class StubConsumer : IConsumer
        {
        	private long _sequence;
        	
            public StubConsumer(long sequence)
            {
                Sequence = sequence;
            }

            public long Sequence 
            {
            	get { return Thread.VolatileRead(ref _sequence); }
            	set { Thread.VolatileWrite(ref _sequence, value); }
            }
            
            public void Halt()
            {
            }
        }
    }
}