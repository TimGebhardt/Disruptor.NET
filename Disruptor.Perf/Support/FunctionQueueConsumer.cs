using System;
using System.Collections.Concurrent;
using System.Threading;

using Disruptor.MemoryLayout;

namespace Disruptor.Perf.Support
{
	public class FunctionQueueConsumer
	{
	    private FunctionStep functionStep;
	    private BlockingCollection<long[]> stepOneQueue;
	    private BlockingCollection<long> stepTwoQueue;
	    private BlockingCollection<long> stepThreeQueue;
	
	    private CacheLineStorageBool running;
	    private CacheLineStorageLong sequence;
	    
	    private long stepThreeCounter;
	
	    public FunctionQueueConsumer(FunctionStep functionStep,
	                                 BlockingCollection<long[]> stepOneQueue,
	                                 BlockingCollection<long> stepTwoQueue,
	                                 BlockingCollection<long> stepThreeQueue)
	    {
	        this.functionStep = functionStep;
	        this.stepOneQueue = stepOneQueue;
	        this.stepTwoQueue = stepTwoQueue;
	        this.stepThreeQueue = stepThreeQueue;
	    }
	    
	    public long StepThreeCounter { get { return stepThreeCounter; } }
	    
	    public void Reset()
	    {
	        stepThreeCounter = 0L;
	        sequence.Data = -1L;
	    }
	    
	    public long Sequence { get { return sequence.Data; } }
	
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
	                    {
	                        long[] values = stepOneQueue.Take();
	                        stepTwoQueue.Add(values[0] + values[1]);
	                        break;
	                    }
	
	                    case FunctionStep.TWO:
	                    {
	                        long value = stepTwoQueue.Take();
	                        stepThreeQueue.Add(value + 3L);
	                        break;
	                    }
	
	                    case FunctionStep.THREE:
	                    {
	                        long value = stepThreeQueue.Take();
	                        if ((value & 4L) == 4L)
	                        {
	                            ++stepThreeCounter;
	                        }
	                        break;
	                    }
	                }
	
	                sequence.Data++;
	            }
	            catch (ThreadInterruptedException)
	            {
	                break;
	            }
	        }
	    }
	}
}
