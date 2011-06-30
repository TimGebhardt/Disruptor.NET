using System;

namespace Disruptor.Test.Support
{
	public class TestEntry : AbstractEntry
	{
	    public override string ToString()
	    {
	        return "Test Entry";
	    }	
	}
	    
	internal class EntryFactory : IEntryFactory<TestEntry>
    {
        public TestEntry Create()
        {
            return new TestEntry();
        }
    }
}
