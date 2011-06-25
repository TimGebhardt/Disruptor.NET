using System;
using System.Diagnostics;
using System.Threading;
using Disruptor.Collections;
using NUnit.Framework;

namespace Disruptor.Test
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
        private static long ITERATIONS = 1000; //* 1000 * 50;
        private static long PAUSE_NANOS = 1000;
        private static Histogram _histogram;
        private Thread thread1;
        private Thread thread2;
        private Thread thread3;


        public Pipeline3StepLatencyPerfTest()
        {
            stepOneConsumerBarrier = ringBuffer.CreateConsumerBarrier();
            stepOneBatchConsumer = new BatchConsumer<FunctionEntry>(stepOneConsumerBarrier, stepOneFunctionHandler);

            stepTwoConsumerBarrier = ringBuffer.CreateConsumerBarrier(stepOneBatchConsumer);
            stepTwoBatchConsumer = new BatchConsumer<FunctionEntry>(stepTwoConsumerBarrier, stepTwoFunctionHandler);

            stepThreeConsumerBarrier = ringBuffer.CreateConsumerBarrier(stepTwoBatchConsumer);
            stepThreeBatchConsumer = new BatchConsumer<FunctionEntry>(stepThreeConsumerBarrier, stepThreeFunctionHandler);

            producerBarrier = ringBuffer.CreateProducerBarrier(stepThreeBatchConsumer);

            //long[] intervals = new long[31];
            //long intervalUpperBound = 1L;
            //for (int i = 0, size = intervals.Length - 1; i < size; i++)
            //{
            //    intervalUpperBound *= 2;
            //    intervals[i] = intervalUpperBound;
            //}

            //intervals[intervals.Length - 1] = long.MaxValue;

            //_histogram = new Histogram(intervals);
        }

        private long nanoTimeCost()
        {
            // long iterations = 10000000;
            //long start = Environment.TickCount;
            //long finish = start;

            //for (int i = 0; i < iterations; i++)
            //{
            //    finish = sw.ElapsedTicks;
            //}

            //if (finish <= start)
            //{
            //    throw new IllegalStateException();
            //}

            //finish = sw.ElapsedTicks;
            //nanoTimeCost = (finish - start) / iterations;
            return 1;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //private  BlockingQueue<Long> stepOneQueue = new ArrayBlockingQueue<Long>(SIZE);
        //private  BlockingQueue<Long> stepTwoQueue = new ArrayBlockingQueue<Long>(SIZE);
        //private  BlockingQueue<Long> stepThreeQueue = new ArrayBlockingQueue<Long>(SIZE);

        //private  LatencyStepQueueConsumer stepOneQueueConsumer =
        //    new LatencyStepQueueConsumer(FunctionStep.ONE, stepOneQueue, stepTwoQueue, histogram, nanoTimeCost);
        //private  LatencyStepQueueConsumer stepTwoQueueConsumer =
        //    new LatencyStepQueueConsumer(FunctionStep.TWO, stepTwoQueue, stepThreeQueue, histogram, nanoTimeCost);
        //private  LatencyStepQueueConsumer stepThreeQueueConsumer =
        //    new LatencyStepQueueConsumer(FunctionStep.THREE, stepThreeQueue, null, histogram, nanoTimeCost);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private readonly IRingBuffer<FunctionEntry> ringBuffer =
            new RingBuffer<FunctionEntry>(new FunctionEntryFactory(), SIZE,
                                          new SingleThreadedStrategy(),
                                          new BusySpinStrategy<FunctionEntry>());

        private readonly IConsumerBarrier<FunctionEntry> stepOneConsumerBarrier;

        private readonly IBatchHandler<FunctionEntry> stepOneFunctionHandler = new FunctionHandler(FunctionStep.ONE);

        private readonly BatchConsumer<FunctionEntry> stepOneBatchConsumer;

        private readonly IConsumerBarrier<FunctionEntry> stepTwoConsumerBarrier;

        private readonly IBatchHandler<FunctionEntry> stepTwoFunctionHandler = new FunctionHandler(FunctionStep.TWO);

        private readonly BatchConsumer<FunctionEntry> stepTwoBatchConsumer;

        private readonly IConsumerBarrier<FunctionEntry> stepThreeConsumerBarrier;

        private readonly IBatchHandler<FunctionEntry> stepThreeFunctionHandler = new FunctionHandler(FunctionStep.THREE);

        private readonly BatchConsumer<FunctionEntry> stepThreeBatchConsumer;

        private readonly IProducerBarrier<FunctionEntry> producerBarrier;
        private Stopwatch _sw;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [Test]
        public void shouldCompareDisruptorVsQueues()

        {
            int RUNS = 1; // 3;

            for (int i = 0; i < RUNS; i++)
            {
                //System.Gc();

                //  histogram.clear();
                runDisruptorPass();
                //      assert.That(Long.valueOf(histogram.getCount()), is(Long.valueOf(ITERATIONS)));
                //         BigDecimal disruptorMeanLatency = histogram.getMean();
                //     System.out.format("%s run %d Disruptor %s\n", getClass().getSimpleName(), Long.valueOf(i), histogram);
                //      dumpHistogram(System.out);

                //histogram.clear();
                //    runQueuePass();
                //     assertThat(Long.valueOf(histogram.getCount()), is(Long.valueOf(ITERATIONS)));
                //           BigDecimal queueMeanLatency = histogram.getMean();
                //      System.out.format("%s run %d Queues %s\n", getClass().getSimpleName(), Long.valueOf(i), histogram);
                //     dumpHistogram(System.out);

                //    assertTrue(queueMeanLatency.compareTo(disruptorMeanLatency) > 0);
            }
        }

        //private void dumpHistogram( PrintStream out)
        //{
        //    for (int i = 0, size = histogram.getSize(); i < size; i++)
        //    {
        //        out.print(histogram.getUpperBoundAt(i));
        //        out.print('\t');
        //        out.print(histogram.getCountAt(i));
        //        out.println();
        //    }
        //}

        private void runDisruptorPass()
        {
            thread1 = new Thread(stepOneBatchConsumer.Run);
            thread2 = new Thread(stepTwoBatchConsumer.Run);
            thread3 = new Thread(stepThreeBatchConsumer.Run);
            thread1.Start();
            thread2.Start();
            thread3.Start();
            Thread.Sleep(100);
            _sw = new Stopwatch();
            _sw.Start();
            for (long i = 0; i < ITERATIONS; i++)
            {
                FunctionEntry entry = producerBarrier.NextEntry();
                entry.setOperandOne(3);
                entry.setOperandTwo(7);
                producerBarrier.Commit(entry);

                long pauseStart = _sw.ElapsedTicks;
                while (PAUSE_NANOS > (_sw.ElapsedTicks - pauseStart))
                {
                    // busy spin
                }
            }

            long expectedSequence = ringBuffer.Cursor;
            while (stepThreeBatchConsumer.Sequence < expectedSequence)
            {
                // busy spin
            }

            stepOneBatchConsumer.Halt();
            stepTwoBatchConsumer.Halt();
            stepThreeBatchConsumer.Halt();
            thread3.Join();
            thread2.Join();
            thread1.Join();
        }

        //private void runQueuePass() throws Exception
        //{
        //    stepThreeQueueConsumer.reset();

        //    Future[] futures = new Future[NUM_CONSUMERS];
        //    futures[0] = EXECUTOR.submit(stepOneQueueConsumer);
        //    futures[1] = EXECUTOR.submit(stepTwoQueueConsumer);
        //    futures[2] = EXECUTOR.submit(stepThreeQueueConsumer);

        //    for (long i = 0; i < ITERATIONS; i++)
        //    {
        //        stepOneQueue.put(Long.valueOf(sw.ElapsedTicks));

        //        long pauseStart = sw.ElapsedTicks;
        //        while (PAUSE_NANOS > (sw.ElapsedTicks -  pauseStart))
        //        {
        //            // busy spin
        //        }
        //    }

        //     long expectedSequence = ITERATIONS - 1;
        //    while (stepThreeQueueConsumer.getSequence() < expectedSequence)
        //    {
        //        // busy spin
        //    }

        //    stepOneQueueConsumer.halt();
        //    stepTwoQueueConsumer.halt();
        //    stepThreeQueueConsumer.halt();

        //    for (Future future : futures)
        //    {
        //        future.cancel(true);
        //    }
        //}
    }

    internal class FunctionEntryFactory : IEntryFactory<FunctionEntry>
    {
        public FunctionEntry Create()
        {
            return new FunctionEntry();
        }
    }

    public class ValueEntry : AbstractEntry
    {
        private long value;

        public long getValue()
        {
            return value;
        }

        public void setValue(long value)
        {
            this.value = value;
        }
    }

    public class LatencyStepHandler : IBatchHandler<ValueEntry>
    {
        private readonly FunctionStep functionStep;
        private Histogram histogram;
        private long nanoTimeCost;

        public LatencyStepHandler(FunctionStep functionStep, Histogram histogram, long nanoTimeCost)
        {
            this.functionStep = functionStep;
            this.histogram = histogram;
            this.nanoTimeCost = nanoTimeCost;
        }


        public void OnAvailable(ValueEntry entry) // throws Exception
        {
            Console.WriteLine(string.Format("{0}:{1}", functionStep, entry.Sequence));
            Thread.Sleep(1);
            switch (functionStep)
            {
                case FunctionStep.ONE:
                case FunctionStep.TWO:
                    break;

                case FunctionStep.THREE:

                    break;
            }
        }

        public void OnEndOfBatch()
        {
            Console.WriteLine(string.Format("{0}:EOB", functionStep));
        }

        public void OnCompletion()
        {
            Console.WriteLine(string.Format("{0}:OnCompletion", functionStep));
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


        public void OnAvailable(FunctionEntry entry) // throws Exception
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
                        Console.WriteLine(stepThreeCounter);
                    }
                    break;
            }
        }

        public void OnEndOfBatch() // throws Exception
        {
        }


        public void OnCompletion()
        {
        }
    }

    internal class ValueEntryFactory : IEntryFactory<ValueEntry>
    {
        public ValueEntry Create()
        {
            return new ValueEntry();
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

    internal class MyClass : IEntryFactory<FunctionEntry>
    {
        public FunctionEntry Create()
        {
            return new FunctionEntry();
        }
    }

    public enum FunctionStep
    {
        ONE,
        TWO,
        THREE
    }
}