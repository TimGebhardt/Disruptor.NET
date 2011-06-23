/**
 * Used to alert consumers waiting at a {@link IConsumerBarrier} of status changes.
 * <P>
 * It does not fill in a stack trace for performance reasons.
 */
//@SuppressWarnings("serial")

using System;

namespace Disruptor
{
    public class AlertException : Exception
    {
        public static readonly AlertException ALERT_EXCEPTION = new AlertException();

        private AlertException()
        {
        }
    }
}