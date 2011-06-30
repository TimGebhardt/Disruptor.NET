using System;
using log4net;
using NUnit.Framework;
using Rhino.Mocks;
using Disruptor;
using Disruptor.Test.Support;

namespace Disruptor.Test
{
	[TestFixture]
	public class IgnoreExceptionHandlerTest
	{
		private MockRepository _mocks = new MockRepository();
	
		[Test]
	    public void shouldHandleAndIgnoreException()
	    {
	        Exception ex = new Exception();
	        IEntry entry = new TestEntry();
	
	        ILog logger = _mocks.DynamicMock<ILog>();
	        
	        Expect.Call(() => logger.Info("Exception processing: " + entry, ex)).Repeat.Never();
	
	        IExceptionHandler exceptionHandler = new IgnoreExceptionHandler(logger);
	        exceptionHandler.Handle(ex, entry);
	    }
	}
}
