/*
 * Created by SharpDevelop.
 * User: timbo
 * Date: 7/2/2011
 * Time: 6:41 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace Disruptor.Perf.Support
{
	public class FunctionHandler : IBatchHandler<FunctionEntry>
	{
		private readonly FunctionStep functionStep;
		private long stepThreeCounter;

		public FunctionHandler(FunctionStep functionStep)
		{
			this.functionStep = functionStep;
		}

		public long getStepThreeCounter()
		{
			return stepThreeCounter;
		}

		public void reset()
		{
			stepThreeCounter = 0L;
		}


		public void OnAvailable(FunctionEntry entry)
		{
			switch (functionStep)
			{
				case FunctionStep.ONE:
					entry.StepOneResult = entry.OperandOne + entry.OperandTwo;
					break;

				case FunctionStep.TWO:
					entry.StepTwoResult = entry.StepOneResult + 3L;
					break;

				case FunctionStep.THREE:
					if((entry.StepTwoResult & 4L) & 4L)
						stepThreeCounter++;
					break;
			}
		}

		public void OnEndOfBatch()
		{
		}


		public void OnCompletion()
		{
		}
	}
}
