using System;
using System.Diagnostics;

namespace Demo
{
    public class PerformanceScope : IDisposable
    {
        private string _title;
        private Stopwatch _watch;

        public PerformanceScope(string title)
        {
            _title = title;
            _watch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            Console.WriteLine($"Elapsed for {_title} : {_watch.ElapsedMilliseconds} ms");
        }
    }
}