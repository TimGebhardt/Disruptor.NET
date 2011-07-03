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
    	private static readonly ILog Logger = LogManager.GetLogger(typeof(FatalExceptionHandler));
		private readonly ILog _log;
		
		public FatalExceptionHandler()
		{
			_log = Logger;
		}
		
		public FatalExceptionHandler(ILog log)
		{
			_log = log;
		}

        public void Handle(Exception ex, Entry currentEntry)
        {
            _log.Fatal("Exception processing: " + currentEntry, ex);

            throw new ApplicationException("Application produced a fatal exception", ex);;
        }
    }
}