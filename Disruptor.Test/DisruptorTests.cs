using System;
using System.Threading;
using NUnit.Framework;

namespace Disruptor.Test
{
    [TestFixture]
    public class DisruptorTests
    {
        [Test]
        public void Test1()
        {
            var testBuffer = new RingBuffer<TestClass>(new TestClassFactory(), 1000, new SingleThreadedStrategy(),
                                                       new BusySpinStrategy<TestClass>());
            IConsumerBarrier<TestClass> consumerBarrier = testBuffer.CreateConsumerBarrier();
            var batchConsumer = new BatchConsumer<TestClass>(consumerBarrier, new TestBatchHandler<TestClass>());
            IProducerBarrier<TestClass> producerBarrier = testBuffer.CreateProducerBarrier(batchConsumer);

            var thread = new Thread(batchConsumer.Run);
            thread.Start();

            for (int i = 0; i < 1000; i++)
            {
                TestClass test = producerBarrier.NextEntry();
                test.Value = i;
                test.Stuff = "FirstTest" + i;

                producerBarrier.Commit(test);
            }

            Thread.Sleep(100);
            batchConsumer.Halt();
            thread.Join();
        }
    }

    public class TestBatchHandler<T> : IBatchHandler<TestClass>
    {
        public void OnAvailable(TestClass entry)
        {
            Console.WriteLine("Available " + entry);
        }

        public void OnEndOfBatch()
        {
            Console.WriteLine("EndofBatch ");
        }

        public void OnCompletion()
        {
            Console.WriteLine("OnCompletion ");
        }
    }

    public class TestClass : AbstractEntry
    {
        public double Value;
        public string Stuff;

        public override string ToString()
        {
            return string.Format("{0} : {1}", Stuff, Value);
        }
    }

    internal class TestClassFactory : IEntryFactory<TestClass>
    {
        public TestClass Create()
        {
            return new TestClass();
        }
    }
}