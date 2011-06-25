/**
 * Strategy employed for making {@link IConsumer}s wait on a {@link RingBuffer}.
 */

using System;
using System.Diagnostics;
using System.Threading;

namespace Disruptor


{
    public interface IWaitStrategy<T> where T : IEntry
    {
        /**
     * Wait for the given sequence to be available for consumption in a {@link RingBuffer}
     *
     * @param consumers further back the chain that must advance first
     * @param ringBuffer on which to wait.
     * @param barrier the consumer is waiting on.
     * @param sequence to be waited on.
     * @return the sequence that is available which may be greater than the requested sequence.
     * @throws AlertException if the status of the Disruptor has changed.
     * @throws InterruptedException if the thread is interrupted.
     */
        long WaitFor(IConsumer[] consumers, IRingBuffer<T> ringBuffer, IConsumerBarrier<T> barrier, long sequence);
        //   throws AlertException, InterruptedException;

        /**
     * Wait for the given sequence to be available for consumption in a {@link RingBuffer} with a timeout specified.
     *
     * @param consumers further back the chain that must advance first
     * @param ringBuffer on which to wait.
     * @param barrier the consumer is waiting on.
     * @param sequence to be waited on.
     * @param timeout value to abort after.
     * @param units of the timeout value.
     * @return the sequence that is available which may be greater than the requested sequence.
     * @throws AlertException if the status of the Disruptor has changed.
     * @throws InterruptedException if the thread is interrupted.
     */

        long WaitFor(IConsumer[] consumers, IRingBuffer<T> ringBuffer, IConsumerBarrier<T> barrier, long sequence,
                     long timeout);

        //  throws AlertException, InterruptedException;

        /**
     * Signal those waiting that the {@link RingBuffer} cursor has advanced.
     */
        void SignalAll();
    }

    /**
     * Blocking strategy that uses a lock and condition variable for {@link IConsumer}s waiting on a barrier.
     *
     * This strategy should be used when performance and low-latency are not as important as CPU resource.
     */

    public class BlockingStrategy<T> : IWaitStrategy<T> where T : IEntry
    {
        private object _lock = new object();
        AutoResetEvent _waithandle = new AutoResetEvent(false);

        public long WaitFor(IConsumer[] consumers, IRingBuffer<T> ringBuffer, IConsumerBarrier<T> barrier, long sequence)
        {
            long availableSequence;
            if ((availableSequence = ringBuffer.Cursor) < sequence)
            {

                lock (_lock)
                {
                    try
                    {
                        while ((availableSequence = ringBuffer.Cursor) < sequence)
                        {
                            if (barrier.IsAlerted())
                            {
                                throw AlertException.ALERT_EXCEPTION;
                            }

                            _waithandle.WaitOne();
                        }
                    }
                    catch (Exception e)
                    {
                    }
                }
            }

            if (0 != consumers.Length)
            {
                while ((availableSequence = Util.GetMinimumSequence(consumers)) < sequence)
                {
                    if (barrier.IsAlerted())
                    {
                        throw AlertException.ALERT_EXCEPTION;
                    }
                }
            }

            return availableSequence;
        }


        public long WaitFor(IConsumer[] consumers, IRingBuffer<T> ringBuffer, IConsumerBarrier<T> barrier,
                            long sequence, long timeout)
        {
            long availableSequence;
            if ((availableSequence = ringBuffer.Cursor) < sequence)
            {
                lock (_lock)
                {
                    try
                    {
                        while ((availableSequence = ringBuffer.Cursor) < sequence)
                        {
                            if (barrier.IsAlerted())
                            {
                                throw AlertException.ALERT_EXCEPTION;
                            }

                            if (!_waithandle.WaitOne((int)timeout))
                            {
                                break;
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }

            if (0 != consumers.Length)
            {
                while ((availableSequence = Util.GetMinimumSequence(consumers)) < sequence)
                {
                    if (barrier.IsAlerted())
                    {
                        throw AlertException.ALERT_EXCEPTION;
                    }
                }
            }

            return availableSequence;
        }


        public void SignalAll()
        {

            lock (_lock)
            {
                try
                {
                    _waithandle.Set();
                }
                catch
                {
                }
            }
        }
    }

    /**
     * Yielding strategy that uses a Thread.yield() for {@link IConsumer}s waiting on a barrier.
     *
     * This strategy is a good compromise between performance and CPU resource.
     */

    public class YieldingStrategy<T> : IWaitStrategy<T> where T : IEntry
    {
        private readonly Stopwatch _timer = Stopwatch.StartNew();

        public long WaitFor(IConsumer[] consumers, IRingBuffer<T> ringBuffer, IConsumerBarrier<T> barrier, long sequence)
        {
            long availableSequence;

            if (0 == consumers.Length)
            {
                while ((availableSequence = ringBuffer.Cursor) < sequence)
                {
                    if (barrier.IsAlerted())
                    {
                        throw AlertException.ALERT_EXCEPTION;
                    }

                    Thread.Yield();
                }
            }
            else
            {
                while ((availableSequence = Util.GetMinimumSequence(consumers)) < sequence)
                {
                    if (barrier.IsAlerted())
                    {
                        throw AlertException.ALERT_EXCEPTION;
                    }

                    Thread.Yield();
                }
            }

            return availableSequence;
        }


        public long WaitFor(IConsumer[] consumers, IRingBuffer<T> ringBuffer, IConsumerBarrier<T> barrier,
                            long sequence, long timeout)
        {
            long currentTime = _timer.ElapsedMilliseconds;
            long cutoff = currentTime + timeout;
            long availableSequence;

            if (0 == consumers.Length)
            {
                while ((availableSequence = ringBuffer.Cursor) < sequence)
                {
                    if (barrier.IsAlerted())
                    {
                        throw AlertException.ALERT_EXCEPTION;
                    }

                    Thread.Yield();
                    if (_timer.ElapsedMilliseconds > cutoff)
                    {
                        break;
                    }
                }
            }
            else
            {
                while ((availableSequence = Util.GetMinimumSequence(consumers)) < sequence)
                {
                    if (barrier.IsAlerted())
                    {
                        throw AlertException.ALERT_EXCEPTION;
                    }

                    Thread.Yield();
                    if (_timer.ElapsedMilliseconds>cutoff)
                    {
                        break;
                    }
                }
            }

            return availableSequence;
        }

       
        public void SignalAll()
        {
        }
    }

    /**
     * Busy Spin strategy that uses a busy spin loop for {@link IConsumer}s waiting on a barrier.
     *
     * This strategy will use CPU resource to avoid syscalls which can introduce latency jitter.  It is best
     * used when threads can be bound to specific CPU cores.
     */

    public class BusySpinStrategy<T> : IWaitStrategy<T> where T : IEntry
    {
        private readonly Stopwatch _timer = Stopwatch.StartNew();

        public long WaitFor(IConsumer[] consumers, IRingBuffer<T> ringBuffer, IConsumerBarrier<T> barrier, long sequence)
        {
            long availableSequence;

            if (0 == consumers.Length)
            {
                while ((availableSequence = ringBuffer.Cursor) < sequence)
                {
                    if (barrier.IsAlerted())
                    {
                        throw AlertException.ALERT_EXCEPTION;
                    }
                }
            }
            else
            {
                while ((availableSequence = Util.GetMinimumSequence(consumers)) < sequence)
                {
                    if (barrier.IsAlerted())
                    {
                        throw AlertException.ALERT_EXCEPTION;
                    }
                }
            }

            return availableSequence;
        }


        public long WaitFor(IConsumer[] consumers, IRingBuffer<T> ringBuffer, IConsumerBarrier<T> barrier,
                            long sequence, long timeout)
        {
            long currentTime = _timer.ElapsedMilliseconds;
            long cutoff = currentTime + timeout;
            long availableSequence;
            if (0 == consumers.Length)
            {
                while ((availableSequence = ringBuffer.Cursor) < sequence)
                {
                    if (barrier.IsAlerted())
                    {
                        throw AlertException.ALERT_EXCEPTION;
                    }

                    if (_timer.ElapsedMilliseconds >= cutoff)
                    {
                        break;
                    }
                }
            }
            else
            {
                while ((availableSequence = Util.GetMinimumSequence(consumers)) < sequence)
                {
                    if (barrier.IsAlerted())
                    {
                        throw AlertException.ALERT_EXCEPTION;
                    }

                    if (_timer.ElapsedMilliseconds >= cutoff)
                    {
                        break;
                    }
                }
            }

            return availableSequence;
        }
        
        public void SignalAll()
        {
        }
    }
}