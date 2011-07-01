using System.Diagnostics;

namespace Disruptor.Perf.Support
{
	/// <summary>
	/// Description of ExtensionMethods.
	/// </summary>
	public static class ExtensionMethods
	{
		public static long GetElapsedNanoSeconds(this Stopwatch stopwatch)
		{
			return stopwatch.ElapsedMilliseconds * 1000000;
		}
	}
}
