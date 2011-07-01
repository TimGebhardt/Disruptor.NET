using System;
using System.Diagnostics;
using Disruptor;
using Disruptor.Collections;

namespace Disruptor.Perf.Support
{
	public class LatencyStepHandler : IBatchHandler<ValueEntry>
	{
	    private FunctionStep functionStep;
	    private readonly Histogram histogram;
	    private readonly long stopwatchTimeCostNs;
	    private readonly Stopwatch stopwatch;
	    
	    public LatencyStepHandler(FunctionStep functionStep, Histogram histogram, long stopwatchTimeCostNs, Stopwatch stopwatch)
	    {
	    	if(!Stopwatch.IsHighResolution)
	    		throw new InvalidOperationException("High resolution timer not available.  Results will not be accurate at all!");

	    	this.functionStep = functionStep;
	        this.histogram = histogram;
	        this.stopwatchTimeCostNs = stopwatchTimeCostNs;
	        this.stopwatch = stopwatch;
	    }
	
	    public void OnAvailable(ValueEntry entry)
	    {
	        switch (functionStep)
	        {
	            case FunctionStep.ONE:
	            case FunctionStep.TWO:
	                break;
	            case FunctionStep.THREE:
					long duration = stopwatch.GetElapsedNanoSeconds() - entry.Value;
	                duration /= 3;
	                duration -= stopwatchTimeCostNs;
	                histogram.AddObservation(duration);
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
