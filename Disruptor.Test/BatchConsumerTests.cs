//using System;
//using System.Threading;
//using NUnit.Framework;

//namespace Disruptor.Test
//{
//    [TestFixture]
//public  class BatchConsumerTest
//{
//   // private  Mockery context = new Mockery();
//  //  private  Sequence lifecycleSequence = context.sequence("lifecycleSequence");
////    private  CountDownLatch latch = new CountDownLatch(1);

//    private  RingBuffer<StubEntry> ringBuffer = new RingBuffer<StubEntry>(new StubFactory(), 16);
//    private  IConsumerBarrier<StubEntry> consumerBarrier;
//     private  IBatchHandler<StubEntry> batchHandler = new BatchHandler
//    private  BatchConsumer<StubEntry> batchConsumer = new BatchConsumer<StubEntry>(consumerBarrier, batchHandler);
//    private  IProducerBarrier<StubEntry> producerBarrier = ringBuffer.CreateProducerBarrier(batchConsumer);

//        public BatchConsumerTest()
//        {
//            consumerBarrier = ringBuffer.CreateConsumerBarrier();
//        }

//        [Test][ExpectedException("NullReferenceException")]
//    public void shouldThrowExceptionOnSettingNullExceptionHandler()
//    {
//        batchConsumer.SetExceptionHandler(null);
//    }

//    [Test]
//    public void shouldReturnUnderlyingBarrier()
//    {
//        Assert.AreEqual(consumerBarrier, batchConsumer.GetConsumerBarrier());
//    }

//    [Test]
//    public void shouldCallMethodsInLifecycleOrder()
//        //throws Exception
//    {
//        context.checking(new Expectations()
//        {
//            {
//                oneOf(batchHandler).OnAvailable(ringBuffer.GetEntry(0));
//                inSequence(lifecycleSequence);

//                oneOf(batchHandler).OnEndOfBatch();
//                inSequence(lifecycleSequence);
//                will(countDown(latch));

//                oneOf(batchHandler).OnCompletion();
//                inSequence(lifecycleSequence);
//            }
//        });

//        Thread thread = new Thread(batchConsumer.Run);
//        thread.Start();

//        Assert.AreEqual(-1L, batchConsumer.Sequence);

//        producerBarrier.Commit(producerBarrier.NextEntry());

//        latch.await();

//        batchConsumer.Halt();
//        thread.Join();
//    }

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
//}
//}

