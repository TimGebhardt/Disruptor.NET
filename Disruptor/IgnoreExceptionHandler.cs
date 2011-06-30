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
    	private static readonly ILog Logger = LogManager.GetLogger(typeof(IgnoreExceptionHandler));
    	private ILog _log;
    	
    	public IgnoreExceptionHandler()
    	{
    		_log = Logger;
    	}
    	
    	public IgnoreExceptionHandler(ILog log)
    	{
    		_log = log;
    	}
    	
        public void Handle(Exception ex, IEntry currentEntry)
        {
            _log.Info("Exception processing: " + currentEntry, ex);
        }
    }
}