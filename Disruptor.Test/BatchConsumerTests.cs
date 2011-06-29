using System;
using System.Threading;
using NUnit.Framework;
using Rhino.Mocks;
using Disruptor.Test.Support;
using Rhino.Mocks.Interfaces;

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

	    [Test]
	    public void ShouldCallMethodsInLifecycleOrderForBatch()
	    {
	    	//You'll notice in these tests we need to actually provide values for the entries
	    	//This is because RhinoMocks is trying to determine what function to call
	    	//based on what arguments are called in.  If the entries producing are all
	    	//equivalent, it'll just call of the expected calls (the one where the argument
	    	//matches the expectation), and the batch will never "catch up" to the ringBuffer.
	    	using(_mocks.Ordered())
	    	{
	    		Expect.Call(() => batchHandler.OnAvailable(ringBuffer.GetEntry(0)));
	    		Expect.Call(() => batchHandler.OnAvailable(ringBuffer.GetEntry(1)));
	    		Expect.Call(() => batchHandler.OnAvailable(ringBuffer.GetEntry(2)));
	    		
	    		Expect.Call(() => batchHandler.OnEndOfBatch()).WhenCalled(m => _latch.Set());
	    		Expect.Call(() => batchHandler.OnCompletion());
	    	}
	    	
	    	_mocks.ReplayAll();
	    	
	    	var entry1 = producerBarrier.NextEntry();
	    	entry1.Value = 100;
	        producerBarrier.Commit(entry1);
	        var entry2 = producerBarrier.NextEntry();
	        entry2.Value = 101;
	        producerBarrier.Commit(entry2);
	        var entry3 = producerBarrier.NextEntry();
	        entry3.Value = 102;
	        producerBarrier.Commit(entry3);
	
	        Thread thread = new Thread(batchConsumer.Run);
	        thread.Start();
	
	        Assert.IsTrue(_latch.WaitOne(TimeSpan.FromSeconds(1)));
	
	        batchConsumer.Halt();
	        thread.Join();
	        
	        _mocks.VerifyAll();
	    }
	
	    [Test]
	    public void ShouldCallExceptionHandlerOnUncaughtException()
	    {
	        Exception ex = new Exception();
	        IExceptionHandler exceptionHandler = _mocks.DynamicMock<IExceptionHandler>();
	        batchConsumer.SetExceptionHandler(exceptionHandler);
	        
	        using(_mocks.Ordered())
	        {
	        	Expect.Call(() => batchHandler.OnAvailable(ringBuffer.GetEntry(0))).Throw(ex);
	        	Expect.Call(() => exceptionHandler.Handle(ex, ringBuffer.GetEntry(0)))
	        		.WhenCalled(m => _latch.Set());
	        	Expect.Call(() => batchHandler.OnCompletion());
	        }
	
	        _mocks.ReplayAll();
	        
	        Thread thread = new Thread(batchConsumer.Run);
	        thread.Start();
	
	        producerBarrier.Commit(producerBarrier.NextEntry());
	
	        Assert.IsTrue(_latch.WaitOne(TimeSpan.FromSeconds(1)));
	
	        batchConsumer.Halt();
	        thread.Join();
	        
	        _mocks.VerifyAll();
	    }
	}
}

