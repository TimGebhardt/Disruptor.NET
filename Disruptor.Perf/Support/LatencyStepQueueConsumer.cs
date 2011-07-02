using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

using Disruptor.Collections;
using Disruptor.MemoryLayout;

namespace Disruptor.Perf.Support
{
	public class LatencyStepQueueConsumer
	{
	    private FunctionStep functionStep;
	
	    private BlockingCollection<long> inputQueue;
	    private BlockingCollection<long> outputQueue;
	    private Histogram histogram;
	    private long nanoTimeCost;
	    private Stopwatch stopwatch;
	
	    private CacheLineStorageBool running;
	    private CacheLineStorageLong sequence;
	
	    public LatencyStepQueueConsumer(FunctionStep functionStep,
	                                    BlockingCollection<long> inputQueue,
	                                    BlockingCollection<long> outputQueue,
	                                    Histogram histogram, 
	                                    long nanoTimeCost,
	                                   Stopwatch stopwatch)
	    {
	        this.functionStep = functionStep;
	        this.inputQueue = inputQueue;
	        this.outputQueue = outputQueue;
	        this.histogram = histogram;
	        this.nanoTimeCost = nanoTimeCost;
	        this.stopwatch = stopwatch;
	    }
	
	    public void Reset()
	    {
	        sequence.Data = -1L;
	    }
	
	    public long GetSequence()
	    {
	        return sequence.Data;
	    }
	
	    public void Halt()
	    {
	        running.Data = false;
	    }
	
	    public void Run()
	    {
	        running.Data = true;
	        while (running.Data)
	        {
	            try
	            {
	                switch (functionStep)
	                {
	                    case FunctionStep.ONE:
	                    case FunctionStep.TWO:
	                    {
	                        outputQueue.Add(inputQueue.Take());
	                        break;
	                    }
	
	                    case FunctionStep.THREE:
	                    {
	                        long value = inputQueue.Take();
	                        long duration = stopwatch.GetElapsedNanoSeconds() - value;
	                        duration /= 3;
	                        duration -= nanoTimeCost;
	                        histogram.AddObservation(duration);
	                        break;
	                    }
	                }
	
	                sequence.Data++;
	            }
	            catch (ThreadInterruptedException	)
	            {
	                break;
	            }
	        }
	    }
	}
}
