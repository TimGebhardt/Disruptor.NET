using System;
using NUnit.Framework;
using Disruptor;
using Disruptor.Collections;

namespace Disruptor.Test.Collections
{
	[TestFixture]
	public class HistogramTest
	{
#if THIRTYTWOBIT
	    public static readonly long[] Intervals = new long[]{ 1, 10, 100, 1000, long.MaxValue-1 };
#else
		public static readonly long[] Intervals = new long[]{ 1, 10, 100, 1000, long.MaxValue };
#endif
	    private Histogram histogram;
	
	    [SetUp]
	    public void SetUp()
	    {
	    	histogram = new Histogram(Intervals);
	    }
	    
	    [Test]
	    public void ShouldSizeBasedOnBucketConfiguration()
	    {
	    	Assert.AreEqual(Intervals.Length, histogram.Size);
	    }
	
	    [Test]
	    public void ShouldWalkIntervals()
	    {
	        for (int i = 0, size = histogram.Size; i < size; i++)
	        {
	        	Assert.AreEqual(Intervals[i], histogram.GetUpperBoundAt(i));
	        }
	    }
	
	    [Test]
	    public void ShouldConfirmIntervalsAreInitialised()
	    {
	        for (int i = 0, size = histogram.Size; i < size; i++)
	        {
	        	Assert.AreEqual(0L, histogram.GetCountAt(i));
	        }
	    }
	
	    [Test][ExpectedException(typeof(ArgumentOutOfRangeException))]
	    public void ShouldThrowExceptionWhenIntervalLessThanOrEqualToZero()
	    {
	        new Histogram(new long[]{-1, 10, 20});
	    }
	
	    [Test][ExpectedException(typeof(ArgumentOutOfRangeException))]
	    public void ShouldThrowExceptionWhenIntervalDoNotIncrease()
	    {
	        new Histogram(new long[]{1, 10, 10, 20});
	    }
	
	    [Test]
	    public void ShouldAddObservation()
	    {
	        Assert.IsTrue(histogram.AddObservation(10L));
	        Assert.AreEqual(1L, histogram.GetCountAt(1));
	    }
	
	    [Test]
	    public void ShouldNotAddObservation()
	    {
	        Assert.IsFalse(new Histogram(new long[]{ 10, 20, 30 }).AddObservation(31));
	    }
	
	    [Test]
	    public void ShouldAddObservations()
	    {
	        AddObservations(histogram, 10L, 30L, 50L);
	
	        Histogram histogram2 = new Histogram(Intervals);
	        AddObservations(histogram2, 10L, 20L, 25L);
	
	        histogram.AddObservations(histogram2);
	
	        Assert.AreEqual(6L, histogram.Count);
	    }
	
	    [Test][ExpectedException(typeof(ArgumentOutOfRangeException))]
	    public void ShouldThrowExceptionWhenIntervalsDoNotMatch()
	    {
	        Histogram histogram2 = new Histogram(new long[]{ 1L, 2L, 3L});
	        histogram.AddObservations(histogram2);
	    }
	
	    [Test]
	    public void ShouldClearCounts()
	    {
	        AddObservations(histogram, 1L, 7L, 10L, 3000L);
	        histogram.Clear();
	
	        for (int i = 0, size = histogram.Size; i < size; i++)
	        {
	        	Assert.AreEqual(0, histogram.GetCountAt(i));
	        }
	    }
	
	    [Test]
	    public void ShouldCountTotalObservations()
	    {
	        AddObservations(histogram, 1L, 7L, 10L, 3000L);
	
	        Assert.AreEqual(4L, histogram.Count);
	    }
	
	    [Test]
	    public void ShouldGetMeanObservation()
	    {
	        long[] intervals = new long[]{ 1, 10, 100, 1000, 10000 };
	        Histogram histogram2 = new Histogram(intervals);
	
	        AddObservations(histogram2, 1L, 7L, 10L, 10L, 11L, 144L);
	
	        Assert.AreEqual(32.666666666666666666666666667m, histogram2.CalculateMean());	    
	    }
	
	    [Test]
	    public void ShouldCorrectMeanForSkewInTopAndBottomPopulatedIntervals()
	    {
	        long[] intervals = new long[]{ 100, 110, 120, 130, 140, 150, 1000, 10000 };
	        Histogram histogram2 = new Histogram(intervals);
	
	        for (long i = 100; i < 152; i++)
	        {
	            histogram2.AddObservation(i);
	        }
	
	        Assert.AreEqual(125.01923076923076923076923077m, histogram2.CalculateMean());
	    }
	
	    [Test]
	    public void ShouldGetMaxObservation()
	    {
	        AddObservations(histogram, 1L, 7L, 10L, 10L, 11L, 144L);
	
	        Assert.AreEqual(144L, histogram.Max);
	    }
	
	    [Test]
	    public void ShouldGetMinObservation()
	    {
	        AddObservations(histogram, 1L, 7L, 10L, 10L, 11L, 144L);
	
	        Assert.AreEqual(1L, histogram.Min);
	    }
	
	    [Test]
	    public void ShouldGetTwoNinesUpperBound()
	    {
	        long[] intervals = new long[]{ 1, 10, 100, 1000, 10000 };
	        Histogram histogram2 = new Histogram(intervals);
	
	        for (long i = 1; i < 101; i++)
	        {
	            histogram2.AddObservation(i);
	        }
	
	        Assert.AreEqual(100L, histogram2.GetTwoNinesUpperBound());
	    }
	
	    [Test]
	    public void ShouldGetFourNinesUpperBound()
	    {
	        long[] intervals = new long[]{ 1, 10, 100, 1000, 10000 };
	        Histogram histogram2 = new Histogram(intervals);
	
	        for (long i = 1; i < 102; i++)
	        {
	            histogram2.AddObservation(i);
	        }
	
	        Assert.AreEqual(1000L, histogram2.GetFourNinesUpperBound());
	    }

	    [Test]
	    public void ShouldToString()
	    {
	        AddObservations(histogram, 1L, 7L, 10L, 300L);
	      
#if THIRTYTWOBIT
	    string expectedResults = "Histogram{min=1, max=300, mean=53.25, 99%=1000, 99.99%=1000, [1=1, 10=2, 100=0, 1000=1, 9223372036854775806=0]}";
#else
		string expectedResults = "Histogram{min=1, max=300, mean=53.25, 99%=1000, 99.99%=1000, [1=1, 10=2, 100=0, 1000=1, 9223372036854775807=0]}";
#endif	
	        Assert.AreEqual(expectedResults, histogram.ToString());
	    }
	
		private void AddObservations(Histogram histogram, params long[] observations)
	    {
	        for (int i = 0, size = observations.Length; i < size; i++)
	        {
	            histogram.AddObservation(observations[i]);
	        }
	    }
	}
}
