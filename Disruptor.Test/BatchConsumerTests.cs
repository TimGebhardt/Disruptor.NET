using System;
using System.Threading;
using NUnit.Framework;
using Rhino.Mocks;

namespace Disruptor.Test
{
	[TestFixture]
	public class BatchConsumerTest
	{
		private MockRepository _mocks;
		private AutoResetEvent _latch;

	    private RingBuffer<StubEntry> ringBuffer;
	    private IConsumerBarrier<StubEntry> consumerBarrier;
	    private IBatchHandler<StubEntry> batchHandler;
	    private BatchConsumer<StubEntry> batchConsumer;
	    private IProducerBarrier<StubEntry> producerBarrier;
        
        [SetUp]
        public void SetUp()
        {
        	_mocks = new MockRepository();
        	_latch = new AutoResetEvent(false);
        	
        	ringBuffer = new RingBuffer<StubEntry>(new StubFactory(), 16);
            consumerBarrier = ringBuffer.CreateConsumerBarrier();
	    	batchHandler = _mocks.DynamicMock<IBatchHandler<StubEntry>>();
	    	batchConsumer = new BatchConsumer<StubEntry>(consumerBarrier, batchHandler);
	    	producerBarrier = ringBuffer.CreateProducerBarrier(batchConsumer);        	
        }

        [Test][ExpectedException(typeof(NullReferenceException))]
	    public void ShouldThrowExceptionOnSettingNullExceptionHandler()
	    {
	        batchConsumer.SetExceptionHandler(null);
	    }

	    [Test]
	    public void ShouldReturnUnderlyingBarrier()
	    {
	        Assert.AreEqual(consumerBarrier, batchConsumer.GetConsumerBarrier());
	    }

	    [Test]
	    public void ShouldCallMethodsInLifecycleOrder()
		{
	    	using(_mocks.Ordered())
	    	{
	    		Expect.Call(() => batchHandler.OnAvailable(ringBuffer.GetEntry(0)));
	    		Expect.Call(() => batchHandler.OnEndOfBatch()).WhenCalled(m => _latch.Set());
	    		Expect.Call(() => batchHandler.OnCompletion());
	    	}

	    	_mocks.ReplayAll();
	    	
	        Thread thread = new Thread(batchConsumer.Run);
	        thread.Start();

	        Assert.AreEqual(-1L, batchConsumer.Sequence);
	        producerBarrier.Commit(producerBarrier.NextEntry());
	        Assert.IsTrue(_latch.WaitOne(TimeSpan.FromSeconds(1)));

	        batchConsumer.Halt();
	        thread.Join();
	        
	        _mocks.VerifyAll();
	    }

//    [Test]
//    public void shouldCallMethodsInLifecycleOrderForBatch()
//      //  throws Exception
//    {
//        context.checking(new Expectations()
//        {
//            {
//                oneOf(batchHandler).OnAvailable(ringBuffer.getEntry(0));
//                inSequence(lifecycleSequence);
//                oneOf(batchHandler).OnAvailable(ringBuffer.getEntry(1));
//                inSequence(lifecycleSequence);
//                oneOf(batchHandler).OnAvailable(ringBuffer.getEntry(2));
//                inSequence(lifecycleSequence);

//                oneOf(batchHandler).OnEndOfBatch();
//                inSequence(lifecycleSequence);
//                will(countDown(latch));

//                oneOf(batchHandler).OnCompletion();
//                inSequence(lifecycleSequence);
//            }
//        });

//        producerBarrier.Commit(producerBarrier.NextEntry());
//        producerBarrier.Commit(producerBarrier.NextEntry());
//        producerBarrier.Commit(producerBarrier.NextEntry());

//        Thread thread = new Thread(batchConsumer.Run);
//        thread.Start();

//        latch.await();

//        batchConsumer.Halt();
//        thread.Join();
//    }

//    [Test]
//    public void shouldCallExceptionHandlerOnUncaughtException()
//     //   throws Exception
//    {
//         Exception ex = new Exception();
//         IExceptionHandler exceptionHandler = new FatalExceptionHandler();
//        batchConsumer.SetExceptionHandler(exceptionHandler);

//        context.checking(new Expectations()
//        {
//            {
//                oneOf(batchHandler).OnAvailable(ringBuffer.GetEntry(0));
//                inSequence(lifecycleSequence);
//                will(new Action()
//                {
//                    @Override
//                    public Object invoke( Invocation invocation) throws Throwable
//                    {
//                        throw ex;
//                    }

//                    @Override
//                    public void describeTo( Description description)
//                    {
//                        description.appendText("Throws exception");
//                    }
//                });

//                oneOf(exceptionHandler).handle(ex, ringBuffer.getEntry(0));
//                inSequence(lifecycleSequence);
//                will(countDown(latch));

//                oneOf(batchHandler).OnCompletion();
//                inSequence(lifecycleSequence);
//            }
//        });

//        Thread thread = new Thread(batchConsumer.Run);
//        thread.Start();

//        producerBarrier.Commit(producerBarrier.NextEntry());

//        latch.await();

//        batchConsumer.Halt();
//        thread.Join();
//    }
	}
}

