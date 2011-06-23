/**
 * Ring based store of reusable entries containing the data representing an {@link IEntry} being exchanged between producers and consumers.
 *
 * @param <T> IEntry implementation storing the data for sharing during exchange or parallel coordination of an event.
 */

using System;
using System.Threading;

namespace Disruptor
{
    public interface IRingBuffer
    {
        int GetCapacity();
        long GetCursor();
    }

    public interface IRingBuffer<T> : IRingBuffer where T : IEntry
    {
        IConsumerBarrier<T> CreateConsumerBarrier(IConsumer[] consumersToTrack);
        IProducerBarrier<T> CreateProducerBarrier(IConsumer[] consumersToTrack);
        IForceFillProducerBarrier<T> CreateForceFillProducerBarrier(IConsumer[] consumersToTrack);
        T GetEntry(long sequence);
    }

    public class RingBuffer<T> : IRingBuffer<T> where T : IEntry
    {
        /** Set to -1 as sequence starting point */
        public /*volatile*/ const long InitialCursorValue = -1L;

        public long p1, p2, p3, p4, p5, p6, p7; // cache line padding

        private /*volatile*/ long _cursor = InitialCursorValue;

        public long p8, p9, p10, p11, p12, p13, p14; // cache line padding

        public readonly object[] _entries;
        private readonly int _ringModMask;

        public readonly IClaimStrategy ClaimStrategy;
        public readonly IWaitStrategy<T> WaitStrategy;

        /**
     * Construct a RingBuffer with the full option set.
     *
     * @param entryFactory to create {@link IEntry}s for filling the RingBuffer
     * @param size of the RingBuffer that will be rounded up to the next power of 2
     * @param claimStrategyOption threading strategy for producers claiming {@link IEntry}s in the ring.
     * @param waitStrategyOption waiting strategy employed by consumers waiting on {@link IEntry}s becoming available.
     */

        public RingBuffer(IEntryFactory<T> entryFactory, int size,
                          IClaimStrategy claimStrategyOption,
                          IWaitStrategy<T> waitStrategyOption)
        {
            int sizeAsPowerOfTwo = Util.CeilingNextPowerOfTwo(size);
            _ringModMask = sizeAsPowerOfTwo - 1;
            _entries = new Object[sizeAsPowerOfTwo];

            ClaimStrategy = claimStrategyOption;
            WaitStrategy = waitStrategyOption;

            Fill(entryFactory);
        }

        /**
     * Construct a RingBuffer with default strategies of:
     * {@link IClaimStrategy.Option#MULTI_THREADED} and {@link IWaitStrategy.Option#BLOCKING}
     *
     * @param entryFactory to create {@link IEntry}s for filling the RingBuffer
     * @param size of the RingBuffer that will be rounded up to the next power of 2
     */

        public RingBuffer(IEntryFactory<T> entryFactory, int size)
            : this(entryFactory, size, new SingleThreadedStrategy(),
                   new YieldingStrategy<T>())
        {
        }

        /**
     * Create a {@link IConsumerBarrier} that gates on the RingBuffer and a list of {@link IConsumer}s
     *
     * @param consumersToTrack this barrier will track
     * @return the barrier gated as required
     */

        public IConsumerBarrier<T> CreateConsumerBarrier(params IConsumer[] consumersToTrack)
        {
            return new ConsumerTrackingConsumerBarrier<T>(this, consumersToTrack);
        }

        /**
     * Create a {@link IProducerBarrier} on this RingBuffer that tracks dependent {@link IConsumer}s.
     *
     * @param consumersToTrack to be tracked to prevent wrapping.
     * @return a {@link IProducerBarrier} with the above configuration.
     */

        public IProducerBarrier<T> CreateProducerBarrier(params IConsumer[] consumersToTrack)
        {
            return new ConsumerTrackingProducerBarrier(this, consumersToTrack);
        }

        /**
     * Create a {@link IForceFillProducerBarrier} on this RingBuffer that tracks dependent {@link IConsumer}s.
     * This barrier is to be used for filling a RingBuffer when no other producers exist.
     *
     * @param consumersToTrack to be tracked to prevent wrapping.
     * @return a {@link IForceFillProducerBarrier} with the above configuration.
     */

        public IForceFillProducerBarrier<T> CreateForceFillProducerBarrier(params IConsumer[] consumersToTrack)
        {
            return new ForceFillConsumerTrackingProducerBarrier(this, consumersToTrack);
        }

        /**
     * The capacity of the RingBuffer to hold entries.
     *
     * @return the size of the RingBuffer.
     */

        public int GetCapacity()
        {
            return _entries.Length;
        }

        /**
     * Get the current sequence that producers have committed to the RingBuffer.
     *
     * @return the current committed sequence.
     */

        public long GetCursor()
        {
            return _cursor;
        }

        /**
     * Get the {@link IEntry} for a given sequence in the RingBuffer.
     *
     * @param sequence for the {@link IEntry}
     * @return {@link IEntry} for the sequence
     */
        //@SuppressWarnings("unchecked")
        public T GetEntry(long sequence)
        {
            return (T) _entries[(int) sequence & _ringModMask];
        }

        private void Fill(IEntryFactory<T> entryEntryFactory)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                _entries[i] = entryEntryFactory.Create();
            }
        }

        /**
     * IConsumerBarrier handed out for gating consumers of the RingBuffer and dependent {@link IConsumer}(s)
     */

        public class ConsumerTrackingConsumerBarrier<T> : IConsumerBarrier<T> where T : IEntry
        {
            public long p1, p2, p3, p4, p5, p6, p7; // cache line padding
            private readonly RingBuffer<T> _ringBuffer;
            private readonly IConsumer[] consumers;
            private volatile bool alerted;
            public long p8, p9, p10, p11, p12, p13, p14; // cache line padding

            public ConsumerTrackingConsumerBarrier(RingBuffer<T> ringBuffer, IConsumer[] consumers)
            {
                _ringBuffer = ringBuffer;
                this.consumers = consumers;
            }


            //     @SuppressWarnings("unchecked")
            public T GetEntry(long sequence)
            {
                return (T) _ringBuffer._entries[(int) sequence & _ringBuffer._ringModMask];
            }


            public long WaitFor(long sequence)
            {
                return _ringBuffer.WaitStrategy.WaitFor(consumers, _ringBuffer, this, sequence);
            }


            public long WaitFor(long sequence, long timeout, TimeUnit units)
                //  throws AlertException, InterruptedException
            {
                return _ringBuffer.WaitStrategy.WaitFor(consumers, _ringBuffer, this, sequence, timeout, units);
            }


            public long GetCursor()
            {
                return _ringBuffer._cursor;
            }


            public bool IsAlerted()
            {
                return alerted;
            }


            public void Alert()
            {
                alerted = true;
                _ringBuffer.WaitStrategy.SignalAll();
            }


            public void ClearAlert()
            {
                alerted = false;
            }
        }

        /**
     * {@link IProducerBarrier} that tracks multiple {@link IConsumer}s when trying to claim
     * a {@link IEntry} in the {@link RingBuffer}.
     */

        public class ConsumerTrackingProducerBarrier : IProducerBarrier<T>
        {
            private readonly RingBuffer<T> _ringBuffer;
            private readonly IConsumer[] consumers;

            public ConsumerTrackingProducerBarrier(RingBuffer<T> ringBuffer, IConsumer[] consumers)
            {
                if (0 == consumers.Length)
                {
                    throw new ArgumentOutOfRangeException(
                        "There must be at least one IConsumer to track for preventing ring wrap");
                }

                _ringBuffer = ringBuffer;
                this.consumers = consumers;
            }


            //    @SuppressWarnings("unchecked")
            public T NextEntry()
            {
                long sequence = _ringBuffer.ClaimStrategy.GetAndIncrement();
                ensureConsumersAreInRange(sequence);

                var entry = (T) _ringBuffer._entries[(int) sequence & _ringBuffer._ringModMask];
                entry.SetSequence(sequence);

                return entry;
            }


            public void Commit(T entry)
            {
                long sequence = entry.GetSequence();
                _ringBuffer.ClaimStrategy.WaitForCursor(sequence - 1L, _ringBuffer);
                _ringBuffer._cursor = sequence;
                _ringBuffer.WaitStrategy.SignalAll();
            }


            public long GetCursor()
            {
                return _ringBuffer._cursor;
            }

            private void ensureConsumersAreInRange(long sequence)
            {
                while ((sequence - Util.GetMinimumSequence(consumers)) >= _ringBuffer._entries.Length)
                {
                    Thread.Yield();
                }
            }
        }

        /**
     * {@link IForceFillProducerBarrier} that tracks multiple {@link IConsumer}s when trying to claim
     * a {@link IEntry} in the {@link RingBuffer}.
     */

        public class ForceFillConsumerTrackingProducerBarrier : IForceFillProducerBarrier<T>
        {
            private readonly IConsumer[] consumers;
            private readonly RingBuffer<T> _ringBuffer;

            public ForceFillConsumerTrackingProducerBarrier(RingBuffer<T> ringBuffer, IConsumer[] consumers)
            {
                if (0 == consumers.Length)
                {
                    throw new ArgumentOutOfRangeException(
                        "There must be at least one IConsumer to track for preventing ring wrap");
                }

                _ringBuffer = ringBuffer;
                this.consumers = consumers;
            }


            //    @SuppressWarnings("unchecked")
            public T ClaimEntry(long sequence)
            {
                ensureConsumersAreInRange(sequence);

                var entry = (T) _ringBuffer._entries[(int) sequence & _ringBuffer._ringModMask];
                entry.SetSequence(sequence);

                return entry;
            }


            public void Commit(T entry)
            {
                long sequence = entry.GetSequence();
                _ringBuffer.ClaimStrategy.SetSequence(sequence + 1L);
                _ringBuffer._cursor = sequence;
                _ringBuffer.WaitStrategy.SignalAll();
            }


            public long GetCursor()
            {
                return _ringBuffer._cursor;
            }

            private void ensureConsumersAreInRange(long sequence)
            {
                while ((sequence - Util.GetMinimumSequence(consumers)) >= _ringBuffer._entries.Length)
                {
                    Thread.Yield();
                }
            }
        }
    }
}