/**
 * Histogram for tracking the frequency of observations of values below interval upper bounds.
 *
 * This class is useful for recording timings in nanoseconds across a large number of observations
 * when high performance is required.
 */

using System;
using System.Text;

namespace Disruptor.Collections
{
    public class Histogram
    {
        private long[] _upperBounds;
        private readonly long[] _counts;
        private long _minValue = long.MaxValue;
        private long _maxValue;

        /**
     * Create a new Histogram with a provided list of interval bounds.
     *
     * @param upperBounds of the intervals.
     */

        public Histogram(long[] upperBounds)
        {
            ValidateBounds(upperBounds);
			
            _upperBounds = new long[upperBounds.Length];
            Array.Copy(upperBounds, _upperBounds, upperBounds.Length);
            _counts = new long[upperBounds.Length];
        }

        private void ValidateBounds(long[] upperBounds)
        {
            long lastBound = -1L;
            foreach (long bound in upperBounds)
            {
                if (bound <= 0L)
                {
                    throw new ArgumentOutOfRangeException("Bounds must be positive values");
                }

                if (bound <= lastBound)
                {
                    throw new ArgumentOutOfRangeException("bound " + bound + " is not greater than " + lastBound);
                }

                lastBound = bound;
            }
        }

        /**
     * Size of the list of interval bars.
     *
     * @return size of the interval bar list.
     */
        public int Size
        {
        	get { return _upperBounds.Length; }
        }

        /**
     * Get the upper bound of an interval for an index.
     *
     * @param index of the upper bound.
     * @return the interval upper bound for the index.
     */

        public long GetUpperBoundAt(int index)
        {
            return _upperBounds[index];
        }

        /**
     * Get the count of observations at a given index.
     *
     * @param index of the observations counter.
     * @return the count of observations at a given index.
     */

        public long GetCountAt(int index)
        {
            return _counts[index];
        }

        /**
     * Add an observation to the histogram and increment the counter for the interval it matches.
     *
     * @param value for the observation to be added.
     * @return return true if in the range of intervals otherwise false.
     */

        public bool AddObservation(long value)
        {
            int low = 0;
            int high = _upperBounds.Length - 1;

            while (low < high)
            {
                int mid = low + ((high - low) >> 1);
                if (_upperBounds[mid] < value)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }

            if (value <= _upperBounds[high])
            {
                _counts[high]++;
                TrackRange(value);

                return true;
            }

            return false;
        }

        private void TrackRange(long value)
        {
            if (value < _minValue)
            {
                _minValue = value;
            }
            else if (value > _maxValue)
            {
                _maxValue = value;
            }
        }

        /**
     * Add observations from another Histogram into this one.
     * Histograms must have the same intervals.
     *
     * @param histogram from which to add the observation counts.
     */

        public void AddObservations(Histogram histogram)
        {
            if (_upperBounds.Length != histogram._upperBounds.Length)
            {
                throw new ArgumentOutOfRangeException("Histograms must have matching intervals");
            }

            for (int i = 0, size = _upperBounds.Length; i < size; i++)
            {
                if (_upperBounds[i] != histogram._upperBounds[i])
                {
                    throw new ArgumentOutOfRangeException("Histograms must have matching intervals");
                }
            }

            for (int i = 0, size = _counts.Length; i < size; i++)
            {
                _counts[i] += histogram._counts[i];
            }

            TrackRange(histogram._minValue);
            TrackRange(histogram._maxValue);
        }

        /**
     * Clear the list of interval counters.
     */

        public void Clear()
        {
            _maxValue = 0L;
            _minValue = long.MaxValue;

            for (int i = 0, size = _counts.Length; i < size; i++)
            {
                _counts[i] = 0L;
            }
        }

        /**
     * Count total number of recorded observations.
     *
     * @return the total number of recorded observations.
     */

        public long Count
        {
        	get 
        	{
	            long count = 0L;
	
	            for (int i = 0, size = _counts.Length; i < size; i++)
	            {
	                count += _counts[i];
	            }
	
	            return count;
        	}
        }

        /**
     * Get the minimum observed value.
     *
     * @return the minimum value observed.
     */

        public long Min
        {
        	get { return _minValue; }
        }

        /**
     * Get the maximum observed value.
     *
     * @return the maximum of the observed values;
     */

        public long Max
        {
        	get { return _maxValue; }
        }

        /**
     * Calculate the mean of all recorded observations.
     *
     * The mean is calculated by the summing the mid points of each interval multiplied by the count
     * for that interval, then dividing by the total count of observations.  The max and min are
     * considered for adjusting the top and bottom bin when calculating the mid point.
     *
     * @return the mean of all recorded observations.
     */

        public decimal CalculateMean()
        {
            long lowerBound = _counts[0] > 0L ? _minValue : 0L;
            decimal total = 0;

            for (int i = 0, size = _upperBounds.Length; i < size; i++)
            {
                long upperBound = Math.Min(_upperBounds[i], _maxValue);
                long midPoint = lowerBound + ((upperBound - lowerBound)/2L);

                decimal intervalTotal = midPoint*_counts[i];
                total += intervalTotal;
                lowerBound = Math.Max(_upperBounds[i] + 1L, _minValue);
            }

            return total/Count;
        }

        /**
     * Calculate the upper bound within which 99% of observations fall.
     *
     * @return the upper bound for 99% of observations.
     */

        public long GetTwoNinesUpperBound()
        {
            return GetUpperBoundForFactor(0.99d);
        }

        /**
     * Calculate the upper bound within which 99.99% of observations fall.
     *
     * @return the upper bound for 99.99% of observations.
     */

        public long GetFourNinesUpperBound()
        {
            return GetUpperBoundForFactor(0.9999d);
        }

        /**
     * Get the interval upper bound for a given factor of the observation population.
     *
     * @param factor representing the size of the population.
     * @return the interval upper bound.
     */

        public long GetUpperBoundForFactor(double factor)
        {
            if (0.0d >= factor || factor >= 1.0d)
            {
                throw new ArgumentOutOfRangeException("factor must be >= 0.0 and <= 1.0");
            }

            long totalCount = Count;
            long tailTotal = totalCount - (long) Math.Round(totalCount*factor);
            long tailCount = 0L;

            for (int i = _counts.Length - 1; i >= 0; i--)
            {
                if (0L != _counts[i])
                {
                    tailCount += _counts[i];
                    if (tailCount >= tailTotal)
                    {
                        return _upperBounds[i];
                    }
                }
            }

            return 0L;
        }


        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("Histogram{");

            sb.Append("min=").Append(Min).Append(", ");
            sb.Append("max=").Append(Max).Append(", ");
            sb.Append("mean=").Append(CalculateMean()).Append(", ");
            sb.Append("99%=").Append(GetTwoNinesUpperBound()).Append(", ");
            sb.Append("99.99%=").Append(GetFourNinesUpperBound()).Append(", ");

            sb.Append('[');
            for (int i = 0, size = _counts.Length; i < size; i++)
            {
                sb.Append(_upperBounds[i]).Append('=').Append(_counts[i]).Append(", ");
            }

            if (_counts.Length > 0)
            {
                sb.Length = (sb.Length - 2);
            }
            sb.Append(']');

            sb.Append('}');

            return sb.ToString();
        }
    }
}