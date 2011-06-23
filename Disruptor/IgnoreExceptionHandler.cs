/**
 * Convenience implementation of an exception handler that using standard JDK logging to log
 * the exception as {@link Level}.INFO
 */

using System;
using log4net;

namespace Disruptor
{
    public class IgnoreExceptionHandler : IExceptionHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger("IgnoreExceptionHandler");
        
        public void Handle(Exception ex, IEntry currentEntry)
        {
            _logger.Error("Exception processing: " + currentEntry, ex);
        }
    }
}