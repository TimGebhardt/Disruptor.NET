using System;
using log4net;
using NUnit.Framework;
using Rhino.Mocks;
using Disruptor.Test.Support;

namespace Disruptor.Test
{
	[TestFixture]
	public class FatalExceptionHandlerTest
	{
		private MockRepository _mocks = new MockRepository();
	
		[Test]
	    public void ShouldHandleFatalException()
	    {
	        Exception causeException = new Exception();
	        IEntry entry = new TestEntry();
	
	        ILog logger = _mocks.DynamicMock<ILog>();
	        
	        Expect.Call(() => logger.Fatal("Exception processing: " + entry, causeException));
            _mocks.ReplayAll();
	        
	        IExceptionHandler exceptionHandler = new FatalExceptionHandler(logger);
	
	        try
	        {
	            exceptionHandler.Handle(causeException, entry);
	            Assert.Fail("No exception was thrown");
	        }
	        catch (ApplicationException ex)
	        {
	            Assert.AreEqual(causeException, ex.InnerException);
	        }
	        
	        _mocks.VerifyAll();
	    }
	}
}
