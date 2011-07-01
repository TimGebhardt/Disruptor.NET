using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Disruptor.Perf;

namespace Disruptor.Perf.Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            Pipeline3StepLatencyPerfTest test = new Pipeline3StepLatencyPerfTest();
            test.ShouldCompareDisruptorVsQueues();
        }
    }
}
