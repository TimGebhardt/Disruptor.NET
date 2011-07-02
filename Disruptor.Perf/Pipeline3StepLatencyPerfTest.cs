using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Disruptor;
using Disruptor.Collections;
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
	 *
	 * Note: <b>This test is only useful on a system using an invariant TSC in user space from the System.nanoTime call.</b>
	 */


	[TestFixture]
	public class Pipeline3StepLatencyPerfTest
	{
		private static int NUM_CONSUMERS = 3;
		private static int SIZE = 1024*32;
		private static long ITERATIONS = 1000;// * 1000; // * 50;
		private static long PAUSE_NANOS = 1000;
		private Histogram _histogram;
		
		//Holds the amount of time that we pay to check Stopwatch.ElapsedTicks so we don't
		//include that in our latency measurements
		private long _stopwatchTimeCostNs;

		//Disruptor testing objects 
		private readonly IRingBuffer<ValueEntry> ringBuffer;
		private readonly IConsumerBarrier<ValueEntry> stepOneConsumerBarrier;
		private LatencyStepHandler stepOneFunctionHandler;
		private readonly BatchConsumer<ValueEntry> stepOneBatchConsumer;
		private readonly IConsumerBarrier<ValueEntry> stepTwoConsumerBarrier;
		private LatencyStepHandler stepTwoFunctionHandler;
		private readonly BatchConsumer<ValueEntry> stepTwoBatchConsumer;
		private readonly IConsumerBarrier<ValueEntry> stepThreeConsumerBarrier;
		private LatencyStepHandler stepThreeFunctionHandler;
		private readonly BatchConsumer<ValueEntry> stepThreeBatchConsumer;
		private readonly IProducerBarrier<ValueEntry> producerBarrier;
		private readonly Stopwatch _stopwatch = new Stopwatch();
		
		//Queue comparison objects
		private BlockingCollection<long> stepOneQueue;
		private BlockingCollection<long> stepTwoQueue;
		private BlockingCollection<long> stepThreeQueue;
		
		private LatencyStepQueueConsumer stepOneQueueConsumer;
		private LatencyStepQueueConsumer stepTwoQueueConsumer;
		private LatencyStepQueueConsumer stepThreeQueueConsumer;

		public Pipeline3StepLatencyPerfTest()
		{
            InitHistogram();
            InitStopwatchTimeCostNs();

			ringBuffer =
				new RingBuffer<ValueEntry>(new ValueEntryFactory(), SIZE,
			                              new SingleThreadedStrategy(),
			                              new BusySpinStrategy<ValueEntry>());
			stepOneConsumerBarrier = ringBuffer.CreateConsumerBarrier();
			stepOneFunctionHandler = new LatencyStepHandler(FunctionStep.ONE, _histogram, _stopwatchTimeCostNs, _stopwatch);
			stepOneBatchConsumer = new BatchConsumer<ValueEntry>(stepOneConsumerBarrier, stepOneFunctionHandler);

			stepTwoConsumerBarrier = ringBuffer.CreateConsumerBarrier(stepOneBatchConsumer);
			stepTwoFunctionHandler = new LatencyStepHandler(FunctionStep.TWO, _histogram, _stopwatchTimeCostNs, _stopwatch);
			stepTwoBatchConsumer = new BatchConsumer<ValueEntry>(stepTwoConsumerBarrier, stepTwoFunctionHandler);

			stepThreeConsumerBarrier = ringBuffer.CreateConsumerBarrier(stepTwoBatchConsumer);
			stepThreeFunctionHandler = new LatencyStepHandler(FunctionStep.THREE, _histogram, _stopwatchTimeCostNs, _stopwatch);
			stepThreeBatchConsumer = new BatchConsumer<ValueEntry>(stepThreeConsumerBarrier, stepThreeFunctionHandler);

			producerBarrier = ringBuffer.CreateProducerBarrier(stepThreeBatchConsumer);
			
			stepOneQueue = new BlockingCollection<long>(SIZE);
			stepTwoQueue = new BlockingCollection<long>(SIZE);
			stepThreeQueue = new BlockingCollection<long>(SIZE);
		
			stepOneQueueConsumer = new LatencyStepQueueConsumer(
				FunctionStep.ONE, stepOneQueue, stepTwoQueue, _histogram, _stopwatchTimeCostNs, _stopwatch);
			stepTwoQueueConsumer = new LatencyStepQueueConsumer(
				FunctionStep.TWO, stepTwoQueue, stepThreeQueue, _histogram, _stopwatchTimeCostNs, _stopwatch);
			stepThreeQueueConsumer = new LatencyStepQueueConsumer(
				FunctionStep.THREE, stepThreeQueue, null, _histogram, _stopwatchTimeCostNs, _stopwatch);
		}
		
		private void InitHistogram()
		{
			long[] intervals = new long[31];
			long intervalUpperBound = 1L;
			for (int i = 0, size = intervals.Length - 1; i < size; i++)
			{
				intervalUpperBound *= 2;
				intervals[i] = intervalUpperBound;
			}

			intervals[intervals.Length - 1] = long.MaxValue;
			_histogram = new Histogram(intervals);
		}
		
		private void InitStopwatchTimeCostNs()
		{
            long iterations = 10000000;
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			
			//the stopwatch does the elapsed time so we don't need to maintain a start/finish
			//set to do the math ourselves.  We do however need a variable to assign to in order
			//make sure the runtime invokes stopwatch.ElapsedTicks in our loop
			long dummy = stopwatch.GetElapsedNanoSeconds();
			
			for (int i = 0; i < iterations; i++)
			{
				dummy = stopwatch.GetElapsedNanoSeconds();
			}
			
			dummy = stopwatch.GetElapsedNanoSeconds();
			stopwatch.Stop();
			_stopwatchTimeCostNs = stopwatch.GetElapsedNanoSeconds() / iterations;
		}

		[Test]
		public void ShouldCompareDisruptorVsQueues()
		{
			int RUNS = 3;

			_stopwatch.Start();
			for (int i = 0; i < RUNS; i++)
			{
				System.GC.Collect();

				_histogram.Clear();
				RunDisruptorPass();
				Assert.AreEqual(ITERATIONS, _histogram.Count);
				var disruptorMeanLatency = _histogram.CalculateMean();
				Console.WriteLine("{0} run {1} Disruptor {2}", GetType().Name, i, _histogram);
				DumpHistogram(Console.Out);

				_histogram.Clear();
				RunQueuePass();
				Assert.AreEqual(ITERATIONS, _histogram.Count);
				decimal queueMeanLatency = _histogram.CalculateMean();
				Console.WriteLine("{0} run {1} Queue {2}\n", GetType().Name, i, _histogram);
				DumpHistogram(Console.Out);

				Assert.IsTrue(queueMeanLatency > disruptorMeanLatency);
			}
		}

		private void DumpHistogram(TextWriter output)
		{
			for (int i = 0, size = _histogram.Size; i < size; i++)
			{
				output.WriteLine("{0}\t{1}", _histogram.GetUpperBoundAt(i), _histogram.GetCountAt(i));
			}
		}

		private void RunDisruptorPass()
		{
			var thread1 = new Thread(stepOneBatchConsumer.Run);
			var thread2 = new Thread(stepTwoBatchConsumer.Run);
			var thread3 = new Thread(stepThreeBatchConsumer.Run);
			thread1.Start();
			thread2.Start();
			thread3.Start();

			for (long i = 0; i < ITERATIONS; i++)
			{
				ValueEntry entry = producerBarrier.NextEntry();
				entry.Value = _stopwatch.GetElapsedNanoSeconds();
				producerBarrier.Commit(entry);

				long pauseStart = _stopwatch.GetElapsedNanoSeconds();
				while (PAUSE_NANOS > (_stopwatch.GetElapsedNanoSeconds() - pauseStart))
				{
					//Busy Spin
				}
			}

			long expectedSequence = ringBuffer.Cursor;
			while (stepThreeBatchConsumer.Sequence < expectedSequence)
			{
				//Busy Spin
			}

			stepOneBatchConsumer.Halt();
			stepTwoBatchConsumer.Halt();
			stepThreeBatchConsumer.Halt();
			thread3.Join();
			thread2.Join();
			thread1.Join();
		}

		private void RunQueuePass() 
		{
		    stepThreeQueueConsumer.Reset();
		    
		    CancellationTokenSource ts = new CancellationTokenSource();
		    CancellationToken ct = ts.Token;
		    Task.Factory.StartNew(stepOneQueueConsumer.Run, ct);
		    Task.Factory.StartNew(stepTwoQueueConsumer.Run, ct);
		    Task.Factory.StartNew(stepThreeQueueConsumer.Run, ct);

		    for (long i = 0; i < ITERATIONS; i++)
		    {
		    	stepOneQueue.Add(_stopwatch.GetElapsedNanoSeconds());

		    	long pauseStart = _stopwatch.GetElapsedNanoSeconds();
		    	while (PAUSE_NANOS > (_stopwatch.GetElapsedNanoSeconds() -  pauseStart))
		        {
		            // busy spin
		        }
		    }

		    long expectedSequence = ITERATIONS - 1;
		    while (stepThreeQueueConsumer.GetSequence() < expectedSequence)
		    {
		        // busy spin
		    }

		    stepOneQueueConsumer.Halt();
		    stepTwoQueueConsumer.Halt();
		    stepThreeQueueConsumer.Halt();

		    ts.Cancel(true);
		}
	}
	
	internal class FunctionEntryFactory : IEntryFactory<FunctionEntry>
	{
		public FunctionEntry Create()
		{
			return new FunctionEntry();
		}
	}

	public class FunctionHandler : IBatchHandler<FunctionEntry>
	{
		private readonly FunctionStep functionStep;
		private long stepThreeCounter;

		public FunctionHandler(FunctionStep functionStep)
		{
			this.functionStep = functionStep;
		}

		public long getStepThreeCounter()
		{
			return stepThreeCounter;
		}

		public void reset()
		{
			stepThreeCounter = 0L;
		}


		public void OnAvailable(FunctionEntry entry)
		{
			switch (functionStep)
			{
				case FunctionStep.ONE:
					entry.setStepOneResult(entry.getOperandOne() + entry.getOperandTwo());
					break;

				case FunctionStep.TWO:
					entry.setStepTwoResult(entry.getStepOneResult() + 3L);
					break;

				case FunctionStep.THREE:
					if ((entry.getStepTwoResult() & 4L) == 4L)
					{
						stepThreeCounter++;
					}
					break;
			}
		}

		public void OnEndOfBatch()
		{
		}


		public void OnCompletion()
		{
		}
	}

	public class FunctionEntry : AbstractEntry
	{
		private long operandOne;
		private long operandTwo;
		private long stepOneResult;
		private long stepTwoResult;

		public long getOperandOne()
		{
			return operandOne;
		}

		public void setOperandOne(long operandOne)
		{
			this.operandOne = operandOne;
		}

		public long getOperandTwo()
		{
			return operandTwo;
		}

		public void setOperandTwo(long operandTwo)
		{
			this.operandTwo = operandTwo;
		}

		public long getStepOneResult()
		{
			return stepOneResult;
		}

		public void setStepOneResult(long stepOneResult)
		{
			this.stepOneResult = stepOneResult;
		}

		public long getStepTwoResult()
		{
			return stepTwoResult;
		}

		public void setStepTwoResult(long stepTwoResult)
		{
			this.stepTwoResult = stepTwoResult;
		}
	}
}
