/**
 * Convenience class for handling the batching semantics of consuming entries from a {@link RingBuffer}
 * and delegating the available {@link Entry}s to a {@link IBatchHandler}.
 *
 * @param <T> IEntry implementation storing the data for sharing during exchange or parallel coordination of an event.
 */

using System;
using System.Threading;
using Disruptor.MemoryLayout;

namespace Disruptor
{
    public class BatchConsumer<T> : IConsumer
        where T : Entry
    {
    	private CacheLineStorageBool _running = new CacheLineStorageBool(true);
    	private CacheLineStorageLong _sequence = new CacheLineStorageLong(-1L);
		
		public long Sequence 
		{
			get { return _sequence.Data; }
		}
		
		private IExceptionHandler _exceptionHandler = new FatalExceptionHandler();
		public IExceptionHandler ExceptionHandler 
		{
			get { return _exceptionHandler; }
			set 
			{
				if (value == null)
            		throw new ArgumentNullException("value", "ExceptionHandler cannot be null");
            	_exceptionHandler = value;
			}
		}
		
        private readonly IConsumerBarrier<T> _consumerBarrier;
        private readonly IBatchHandler<T> _handler;
        private readonly bool _noSequenceTracker;

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

        public IConsumerBarrier<T> GetConsumerBarrier()
        {
            return _consumerBarrier;
        }

        public void Halt()
        {
            _running.Data = false;
            _consumerBarrier.Alert();
        }

		/// <summary>
		/// Runs this batch consumer.
		/// </summary>
		/// <remarks>
		/// It is ok to have another thread rerun this method after a Halt().
		/// </remarks>
        public void Run()
        {
            _running.Data = true;
            T entry = default(T);

            while (_running.Data)
            {
                try
                {
                    long nextSequence = Sequence + 1;
                    long availableSeq = _consumerBarrier.WaitFor(nextSequence);

                    for (long i = nextSequence; i <= availableSeq; i++)
                    {
                        entry = _consumerBarrier.GetEntry(i);
                        _handler.OnAvailable(entry);

                        if (_noSequenceTracker)
                        {
                            _sequence.Data = entry.Sequence;
                        }
                    }

                    _handler.OnEndOfBatch();
                }
                //TODO: Exception handling in Java can be fast if you don't grab a stack trace,
                //But it's really slow in .NET.  Figure out a way to port this to work better in
                //.NET
                catch (AlertException)
                {
                    // Wake up from blocking wait and check if we should continue to run
                }
                catch (Exception ex)
                {
                    _exceptionHandler.Handle(ex, entry);
                    if (_noSequenceTracker)
                    {
                        _sequence.Data = entry.Sequence;
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
                _batchConsumer._sequence.Data = sequence;
            }
        }
    }
}