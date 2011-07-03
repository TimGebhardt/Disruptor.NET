using System;
using Disruptor;

namespace Disruptor.Perf.Support
{
	public class ValueEntry : Entry
	{
		public long Value { get; set; }
	}
	
	internal class ValueEntryFactory : IEntryFactory<ValueEntry>
	{
		public ValueEntry Create()
		{
			return new ValueEntry();
		}
	}

}
