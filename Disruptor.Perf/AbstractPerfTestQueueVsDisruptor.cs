using System;
using NUnit.Framework;

namespace Disruptor.Perf
{
	public abstract class AbstractPerfTestQueueVsDisruptor
	{
	    protected void TestImplementations()
	    {
	        int RUNS = 3;
	        double disruptorOps = 0.0;
	        double queueOps = 0.0;
	
	        for (int i = 0; i < RUNS; i++)
	        {
	        	System.GC.Collect();
	
	            disruptorOps = RunDisruptorPass(i);
	            queueOps = RunQueuePass(i);
	
	            PrintResults(GetType(), disruptorOps, queueOps, i);
	        }
	
	        Assert.Greater(disruptorOps, queueOps, "Performance degraded");
	    }
	
	
	    public static void PrintResults(Type testType, double disruptorOps, double queueOps, int run)
	    {
	    	Console.WriteLine("\n{0} OpsPerSecond run {1}: BlockingQueues={2:0.##}, Disruptor={3:0.##}\n",
	    	                  testType.Name, run, queueOps, disruptorOps);
	    }
	
	    protected abstract double RunQueuePass(int passNumber);
	
	    protected abstract double RunDisruptorPass(int passNumber);
	
	    protected abstract void ShouldCompareDisruptorVsQueues();
	}
}
