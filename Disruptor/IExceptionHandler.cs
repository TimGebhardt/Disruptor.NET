/**
 * Callback handler for uncaught exceptions in the {@link IEntry} processing cycle of the {@link BatchConsumer}
 */

using System;

namespace Disruptor
{
    public interface IExceptionHandler
    {
        void Handle(Exception ex, IEntry currentEntry);
    }
}