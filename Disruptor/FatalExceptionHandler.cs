/**
 * Convenience implementation of an exception handler that using standard JDK logging to log
 * the exception as {@link Level}.SEVERE and re-throw it wrapped in a {@link RuntimeException}
 */

using System;
using log4net;

namespace Disruptor
{
    public class FatalExceptionHandler : IExceptionHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger("FatalException");


        public void Handle(Exception ex, IEntry currentEntry)
        {
            _logger.Fatal("Exception processing: " + currentEntry, ex);

            throw ex;
        }
    }
}