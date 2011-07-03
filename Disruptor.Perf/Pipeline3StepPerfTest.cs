using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Disruptor.Perf.Support;

namespace Disruptor.Perf
{
	/**
	 * <pre>
	 *
	 * Pipeline a series of stages from a producer to ultimate consumer.
	 * Each consumer depends on the output of the previous consumer.
	 *
	 * +----+    +----+    +----+    +----+
	 * | P0 |--->| C0 |--->| C1 |--->| C2 |
	 * +----+    +----+    +----+    +----+
	 *
	 *
	 * Queue Based:
	 * ============
	 *
	 *        put      take       put      take       put      take
	 * +----+    +====+    +----+    +====+    +----+    +====+    +----+
	 * | P0 |--->| Q0 |<---| C0 |--->| Q1 |<---| C1 |--->| Q2 |<---| C2 |
	 * +----+    +====+    +----+    +====+    +----+    +====+    +----+
	 *
	 * P0 - Producer 0
	 * Q0 - Queue 0
	 * C0 - Consumer 0
	 * Q1 - Queue 1
	 * C1 - Consumer 1
	 * Q2 - Queue 2
	 * C2 - Consumer 1
	 *
	 *
	 * Disruptor:
	 * ==========
	 *                   track to prevent wrap
	 *             +------------------------------------------------------------------------+
	 *             |                                                                        |
	 *             |                                                                        v
	 * +----+    +====+    +====+    +=====+    +----+    +=====+    +----+    +=====+    +----+
	 * | P0 |--->| PB |--->| RB |    | CB0 |<---| C0 |<---| CB1 |<---| C1 |<---| CB2 |<---| C2 |
	 * +----+    +====+    +====+    +=====+    +----+    +=====+    +----+    +=====+    +----+
	 *                claim   ^  get    |  waitFor           |  waitFor           |  waitFor
	 *                        |         |                    |                    |
	 *                        +---------+--------------------+--------------------+
	 *
	 *
	 * P0  - Producer 0
	 * PB  - ProducerBarrier
	 * RB  - RingBuffer
	 * CB0 - ConsumerBarrier 0
	 * C0  - Consumer 0
	 * CB1 - ConsumerBarrier 1
	 * C1  - Consumer 1
	 * CB2 - ConsumerBarrier 2
	 * C2  - Consumer 2
	 *
	 * </pre>
	 */
	[TestFixture]
	public class Pipeline3StepPerfTest : AbstractPerfTestQueueVsDisruptor
	{
	    private static int NUM_CONSUMERS = 3;
	    private static int SIZE = 1024 * 32;
	    private static long ITERATIONS = 1000 * 1000;// * 500;
	
	    private static long OPERAND_TWO_INITIAL_VALUE = 777L;
	    private long expectedResult;
	    
	    private void InitExpectedResult()
	    {
	        long temp = 0L;
	        long operandTwo = OPERAND_TWO_INITIAL_VALUE;
	
	        for (long i = 0; i < ITERATIONS; i++)
	        {
	            long stepOneResult = i + operandTwo--;
	            long stepTwoResult = stepOneResult + 3;
	
	            if ((stepTwoResult & 4L) == 4L)
	            {
	                ++temp;
	            }
	        }
	
	        expectedResult = temp;
	    }
	    
	    private void InitDisruptorObjects()
	    {
	    	ringBuffer = new RingBuffer<FunctionEntry>(new FunctionEntryFactory(), SIZE,
	    	                                           new SingleThreadedStrategy(),
	    	                                           new BusySpinStrategy<FunctionEntry>());
		
		    stepOneConsumerBarrier = ringBuffer.CreateConsumerBarrier();
		    stepOneFunctionHandler = new FunctionHandler(FunctionStep.ONE);
		    stepOneBatchConsumer = new BatchConsumer<FunctionEntry>(stepOneConsumerBarrier, stepOneFunctionHandler);
		
		    stepTwoConsumerBarrier = ringBuffer.CreateConsumerBarrier(stepOneBatchConsumer);
		    stepTwoFunctionHandler = new FunctionHandler(FunctionStep.TWO);
		    stepTwoBatchConsumer = new BatchConsumer<FunctionEntry>(stepTwoConsumerBarrier, stepTwoFunctionHandler);
		
		    stepThreeConsumerBarrier = ringBuffer.CreateConsumerBarrier(stepTwoBatchConsumer);
		    stepThreeFunctionHandler = new FunctionHandler(FunctionStep.THREE);
		    stepThreeBatchConsumer = new BatchConsumer<FunctionEntry>(stepThreeConsumerBarrier, stepThreeFunctionHandler);
		
		    producerBarrier = ringBuffer.CreateProducerBarrier(stepThreeBatchConsumer);	    	
	    }
	
	    ///////////////////////////////////////////////////////////////////////////////////////////////
	
//	    private final BlockingQueue<long[]> stepOneQueue = new ArrayBlockingQueue<long[]>(SIZE);
//	    private final BlockingQueue<Long> stepTwoQueue = new ArrayBlockingQueue<Long>(SIZE);
//	    private final BlockingQueue<Long> stepThreeQueue = new ArrayBlockingQueue<Long>(SIZE);
//	
//	    private final FunctionQueueConsumer stepOneQueueConsumer =
//	        new FunctionQueueConsumer(FunctionStep.ONE, stepOneQueue, stepTwoQueue, stepThreeQueue);
//	    private final FunctionQueueConsumer stepTwoQueueConsumer =
//	        new FunctionQueueConsumer(FunctionStep.TWO, stepOneQueue, stepTwoQueue, stepThreeQueue);
//	    private final FunctionQueueConsumer stepThreeQueueConsumer =
//	        new FunctionQueueConsumer(FunctionStep.THREE, stepOneQueue, stepTwoQueue, stepThreeQueue);
	
	    ///////////////////////////////////////////////////////////////////////////////////////////////
	
	    //Disruptor objects
	    private IRingBuffer<FunctionEntry> ringBuffer;
	    
	    private IConsumerBarrier<FunctionEntry> stepOneConsumerBarrier;
	    private FunctionHandler stepOneFunctionHandler;
	    private BatchConsumer<FunctionEntry> stepOneBatchConsumer;
	
	    private IConsumerBarrier<FunctionEntry> stepTwoConsumerBarrier;
	    private FunctionHandler stepTwoFunctionHandler;
	    private BatchConsumer<FunctionEntry> stepTwoBatchConsumer;
	
	    private IConsumerBarrier<FunctionEntry> stepThreeConsumerBarrier;
	    private FunctionHandler stepThreeFunctionHandler;
	    private BatchConsumer<FunctionEntry> stepThreeBatchConsumer;
	
	    private IProducerBarrier<FunctionEntry> producerBarrier;
	
	    ///////////////////////////////////////////////////////////////////////////////////////////////
	
	    [Test]
	    public void ShouldCompareDisruptorVsQueuesPublic()
	    {
	    	ShouldCompareDisruptorVsQueues();
	    }
	    
	    protected override void ShouldCompareDisruptorVsQueues()
	    {
	    	InitExpectedResult();
	    	InitDisruptorObjects();
	        TestImplementations();
	    }
	
	    protected override double RunDisruptorPass(int passNumber)
	    {
	        stepThreeFunctionHandler.reset();
	
	        Task.Factory.StartNew(stepOneBatchConsumer.Run);
	        Task.Factory.StartNew(stepTwoBatchConsumer.Run);
	        Task.Factory.StartNew(stepThreeBatchConsumer.Run);
	
	        var stopwatch = new Stopwatch();
	        stopwatch.Start();
	
	        long operandTwo = OPERAND_TWO_INITIAL_VALUE;
	        for (long i = 0; i < ITERATIONS; i++)
	        {
	            FunctionEntry entry = producerBarrier.NextEntry();
	            entry.OperandOne = i;
	            entry.OperandTwo = operandTwo--;
	            producerBarrier.Commit(entry);
	        }
	
	        long expectedSequence = ringBuffer.Cursor;
	        while (stepThreeBatchConsumer.Sequence < expectedSequence)
	        {
	            // busy spin
	        }
	
	        stopwatch.Stop();
	        double opsPerSecond = ITERATIONS / stopwatch.Elapsed.TotalSeconds;
	
	        stepOneBatchConsumer.Halt();
	        stepTwoBatchConsumer.Halt();
	        stepThreeBatchConsumer.Halt();
	
	        Assert.AreEqual(expectedResult, stepThreeFunctionHandler.getStepThreeCounter());
	
	        return opsPerSecond;
	    }
	
	    protected override double RunQueuePass(int passNumber)
	    {
	    	return 0.0;
//	        stepThreeQueueConsumer.reset();
//	
//	        Future[] futures = new Future[NUM_CONSUMERS];
//	        futures[0] = EXECUTOR.submit(stepOneQueueConsumer);
//	        futures[1] = EXECUTOR.submit(stepTwoQueueConsumer);
//	        futures[2] = EXECUTOR.submit(stepThreeQueueConsumer);
//	
//	        long start = System.currentTimeMillis();
//	
//	        long operandTwo = OPERAND_TWO_INITIAL_VALUE;
//	        for (long i = 0; i < ITERATIONS; i++)
//	        {
//	            long[] values = new long[2];
//	            values[0] = i;
//	            values[1] = operandTwo--;
//	            stepOneQueue.put(values);
//	        }
//	
//	        final long expectedSequence = ITERATIONS - 1;
//	        while (stepThreeQueueConsumer.getSequence() < expectedSequence)
//	        {
//	            // busy spin
//	        }
//	
//	        long opsPerSecond = (ITERATIONS * 1000L) / (System.currentTimeMillis() - start);
//	
//	        stepOneQueueConsumer.halt();
//	        stepTwoQueueConsumer.halt();
//	        stepThreeQueueConsumer.halt();
//	
//	        for (Future future : futures)
//	        {
//	            future.cancel(true);
//	        }
//	
//	        Assert.assertEquals(expectedResult, stepThreeQueueConsumer.getStepThreeCounter());
//	
//	        return opsPerSecond;
	    }
	}

}
