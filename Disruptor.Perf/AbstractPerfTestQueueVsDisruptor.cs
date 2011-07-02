using System;
using NUnit.Framework;

namespace Disruptor.Perf
{
	public abstract class AbstractPerfTestQueueVsDisruptor
	{
	    protected void TestImplementations()
	    {
	        int RUNS = 3;
	        long disruptorOps = 0L;
	        long queueOps = 0L;
	
	        for (int i = 0; i < RUNS; i++)
	        {
	        	System.GC.Collect();
	
	            disruptorOps = RunDisruptorPass(i);
	            queueOps = RunQueuePass(i);
	
	            PrintResults(GetType(), disruptorOps, queueOps, i);
	        }
	
	        Assert.Greater(disruptorOps, queueOps, "Performance degraded");
	    }
	
	
	    public static void PrintResults(Type testType, long disruptorOps, long queueOps, int run)
	    {
	    	Console.WriteLine("\n{0} OpsPerSecond run {1}: BlockingQueues={2}, Disruptor={3}\n",
	    	                  testType.Name, run, queueOps, disruptorOps);
	    }
	
	    protected abstract long RunQueuePass(int passNumber);
	
	    protected abstract long RunDisruptorPass(int passNumber);
	
	    protected abstract void ShouldCompareDisruptorVsQueues();
	}
}
