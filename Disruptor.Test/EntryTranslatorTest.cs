using System;
using NUnit.Framework;
using Disruptor.Test.Support;

namespace Disruptor.Test
{
	[TestFixture]
	public class EntryTranslatorTest
	{
	    private static readonly string TEST_VALUE = "Wibble";
	
	    [Test]
	    public void ShouldTranslateOtherDataIntoAnEntry()
	    {
	    	StubEntry entry = new StubFactory().Create();
	        IEntryTranslator<StubEntry> entryTranslator = new ExampleEntryTranslator(TEST_VALUE);
	
	        entry = entryTranslator.TranslateTo(entry);
	
	        Assert.AreEqual(TEST_VALUE, entry.teststring);
	    }
	
	    public class ExampleEntryTranslator : IEntryTranslator<StubEntry>
	    {
	        private readonly string _testValue;
	
	        public ExampleEntryTranslator(string testValue)
	        {
	            this._testValue = testValue;
	        }
	
	        public StubEntry TranslateTo(StubEntry entry)
	        {
	            entry.teststring = _testValue;
	            return entry;
	        }
	    }
	}
}
