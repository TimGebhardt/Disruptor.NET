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
        private long[] upperBounds;
        private readonly long[] counts;
        private long minValue = long.MaxValue;
        private long maxValue;

        /**
     * Create a new Histogram with a provided list of interval bounds.
     *
     * @param upperBounds of the intervals.
     */

        public Histogram(long[] upperBounds)
        {
            validateBounds(upperBounds);

            Array.Copy(upperBounds, this.upperBounds, upperBounds.Length);
            counts = new long[upperBounds.Length];
        }

        private void validateBounds(long[] upperBounds)
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

        public int getSize()
        {
            return upperBounds.Length;
        }

        /**
     * Get the upper bound of an interval for an index.
     *
     * @param index of the upper bound.
     * @return the interval upper bound for the index.
     */

        public long getUpperBoundAt(int index)
        {
            return upperBounds[index];
        }

        /**
     * Get the count of observations at a given index.
     *
     * @param index of the observations counter.
     * @return the count of observations at a given index.
     */

        public long getCountAt(int index)
        {
            return counts[index];
        }

        /**
     * Add an observation to the histogram and increment the counter for the interval it matches.
     *
     * @param value for the observation to be added.
     * @return return true if in the range of intervals otherwise false.
     */

        public bool addObservation(long value)
        {
            int low = 0;
            int high = upperBounds.Length - 1;

            while (low < high)
            {
                int mid = low + ((high - low) >> 1);
                if (upperBounds[mid] < value)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }

            if (value <= upperBounds[high])
            {
                counts[high]++;
                trackRange(value);

                return true;
            }

            return false;
        }

        private void trackRange(long value)
        {
            if (value < minValue)
            {
                minValue = value;
            }
            else if (value > maxValue)
            {
                maxValue = value;
            }
        }

        /**
     * Add observations from another Histogram into this one.
     * Histograms must have the same intervals.
     *
     * @param histogram from which to add the observation counts.
     */

        public void addObservations(Histogram histogram)
        {
            if (upperBounds.Length != histogram.upperBounds.Length)
            {
                throw new ArgumentOutOfRangeException("Histograms must have matching intervals");
            }

            for (int i = 0, size = upperBounds.Length; i < size; i++)
            {
                if (upperBounds[i] != histogram.upperBounds[i])
                {
                    throw new ArgumentOutOfRangeException("Histograms must have matching intervals");
                }
            }

            for (int i = 0, size = counts.Length; i < size; i++)
            {
                counts[i] += histogram.counts[i];
            }

            trackRange(histogram.minValue);
            trackRange(histogram.maxValue);
        }

        /**
     * Clear the list of interval counters.
     */

        public void clear()
        {
            maxValue = 0L;
            minValue = long.MaxValue;

            for (int i = 0, size = counts.Length; i < size; i++)
            {
                counts[i] = 0L;
            }
        }

        /**
     * Count total number of recorded observations.
     *
     * @return the total number of recorded observations.
     */

        public long getCount()
        {
            long count = 0L;

            for (int i = 0, size = counts.Length; i < size; i++)
            {
                count += counts[i];
            }

            return count;
        }

        /**
     * Get the minimum observed value.
     *
     * @return the minimum value observed.
     */

        public long getMin()
        {
            return minValue;
        }

        /**
     * Get the maximum observed value.
     *
     * @return the maximum of the observed values;
     */

        public long getMax()
        {
            return maxValue;
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

        public decimal getMean()
        {
            long lowerBound = counts[0] > 0L ? minValue : 0L;
            decimal total = 0;

            for (int i = 0, size = upperBounds.Length; i < size; i++)
            {
                long upperBound = Math.Min(upperBounds[i], maxValue);
                long midPoint = lowerBound + ((upperBound - lowerBound)/2L);

                decimal intervalTotal = midPoint*counts[i];
                total += intervalTotal;
                lowerBound = Math.Max(upperBounds[i] + 1L, minValue);
            }

            return total/getCount();
        }

        /**
     * Calculate the upper bound within which 99% of observations fall.
     *
     * @return the upper bound for 99% of observations.
     */

        public long getTwoNinesUpperBound()
        {
            return getUpperBoundForFactor(0.99d);
        }

        /**
     * Calculate the upper bound within which 99.99% of observations fall.
     *
     * @return the upper bound for 99.99% of observations.
     */

        public long getFourNinesUpperBound()
        {
            return getUpperBoundForFactor(0.9999d);
        }

        /**
     * Get the interval upper bound for a given factor of the observation population.
     *
     * @param factor representing the size of the population.
     * @return the interval upper bound.
     */

        public long getUpperBoundForFactor(double factor)
        {
            if (0.0d >= factor || factor >= 1.0d)
            {
                throw new ArgumentOutOfRangeException("factor must be >= 0.0 and <= 1.0");
            }

            long totalCount = getCount();
            long tailTotal = totalCount - (long) Math.Round(totalCount*factor);
            long tailCount = 0L;

            for (int i = counts.Length - 1; i >= 0; i--)
            {
                if (0L != counts[i])
                {
                    tailCount += counts[i];
                    if (tailCount >= tailTotal)
                    {
                        return upperBounds[i];
                    }
                }
            }

            return 0L;
        }


        public string toString()
        {
            var sb = new StringBuilder();

            sb.Append("Histogram{");

            sb.Append("min=").Append(getMin()).Append(", ");
            sb.Append("max=").Append(getMax()).Append(", ");
            sb.Append("mean=").Append(getMean()).Append(", ");
            sb.Append("99%=").Append(getTwoNinesUpperBound()).Append(", ");
            sb.Append("99.99%=").Append(getFourNinesUpperBound()).Append(", ");

            sb.Append('[');
            for (int i = 0, size = counts.Length; i < size; i++)
            {
                sb.Append(upperBounds[i]).Append('=').Append(counts[i]).Append(", ");
            }

            if (counts.Length > 0)
            {
                sb.Length = (sb.Length - 2);
            }
            sb.Append(']');

            sb.Append('}');

            return sb.ToString();
        }
    }
}