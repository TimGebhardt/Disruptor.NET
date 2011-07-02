/*
 * Created by SharpDevelop.
 * User: timbo
 * Date: 7/2/2011
 * Time: 6:35 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace Disruptor.Perf.Support
{
	public class FunctionEntry : AbstractEntry
	{
		public long OperandOne { get; set; }
		public long OperandTwo { get; set; }
		public long StepOneResult { get; set; }
		public long StepTwoResult { get; set; }
	}
	
	internal class FunctionEntryFactory : IEntryFactory<FunctionEntry>
	{
		public FunctionEntry Create()
		{
			return new FunctionEntry();
		}
	}
}
