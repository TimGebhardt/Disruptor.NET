/**
 * Convenience class for handling the batching semantics of consuming entries from a {@link RingBuffer}
 * and delegating the available {@link Entry}s to a {@link IBatchHandler}.
 *
 * @param <T> IEntry implementation storing the data for sharing during exchange or parallel coordination of an event.
 */

using System;

namespace Disruptor
{
    public class BatchConsumer<T> : IConsumer
        where T : IEntry
    {
        public long p1, p2, p3, p4, p5, p6, p7; // cache line padding
        private volatile bool _running = true;
        public long p8, p9, p10, p11, p12, p13, p14; // cache line padding
        private /*volatile*/ long _sequence = -1L;
        public long p15, p16, p17, p18, p19, p20, p21;

        private readonly IConsumerBarrier<T> _consumerBarrier;
        private readonly IBatchHandler<T> _handler;
        private readonly bool _noSequenceTracker;
        private IExceptionHandler _exceptionHandler = new FatalExceptionHandler();

        public BatchConsumer(IConsumerBarrier<T> consumerBarrier,
                             IBatchHandler<T> handler)
        {
            _consumerBarrier = consumerBarrier;
            _handler = handler;
            _noSequenceTracker = true;
        }

        public BatchConsumer(IConsumerBarrier<T> consumerBarrier,
                             ISequenceTrackingHandler<T> entryHandler)
        {
            _consumerBarrier = consumerBarrier;
            _handler = entryHandler;

            _noSequenceTracker = false;
            entryHandler.SetSequenceTrackerCallback(new SequenceTrackerCallback(this));
        }

        public void setExceptionHandler(IExceptionHandler exceptionHandler)
        {
            if (null == exceptionHandler)
            {
                throw new NullReferenceException();
            }

            _exceptionHandler = exceptionHandler;
        }

        public IConsumerBarrier<T> GetConsumerBarrier()
        {
            return _consumerBarrier;
        }


        public long GetSequence()
        {
            return _sequence;
        }


        public void Halt()
        {
            _running = false;
            _consumerBarrier.Alert();
        }

// It is ok to have another thread rerun this method after a halt().
        public void Run()
        {
            _running = true;
            T entry = default(T);

            while (_running)
            {
                try
                {
                    long nextSequence = _sequence + 1;
                    long availableSeq = _consumerBarrier.WaitFor(nextSequence);

                    for (long i = nextSequence; i <= availableSeq; i++)
                    {
                        entry = _consumerBarrier.GetEntry(i);
                        _handler.OnAvailable(entry);

                        if (_noSequenceTracker)
                        {
                            _sequence = entry.GetSequence();
                        }
                    }

                    _handler.OnEndOfBatch();
                }
                catch (AlertException ex)
                {
                    // Wake up from blocking wait and check if we should continue to run
                }
                catch (Exception ex)
                {
                    _exceptionHandler.Handle(ex, entry);
                    if (_noSequenceTracker)
                    {
                        _sequence = entry.GetSequence();
                    }
                }
            }

            _handler.OnCompletion();
        }

        public class SequenceTrackerCallback
        {
            private readonly BatchConsumer<T> _batchConsumer;

            public SequenceTrackerCallback(BatchConsumer<T> batchConsumer)
            {
                _batchConsumer = batchConsumer;
            }

            public void OnCompleted(long sequence)
            {
                _batchConsumer._sequence = sequence;
            }
        }
    }
}