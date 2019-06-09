using System;
using System.Diagnostics;

namespace FhiModel.Common
{
    public class Measure: IDisposable
    {
        private readonly String _name;
        private readonly DateTime _start;

        public Measure(String name)
        {
            _name = name;
            _start = DateTime.Now;
        }

        public void Dispose()
        {
            Trace.WriteLine($"{_name}; E={DateTime.Now}; d={(DateTime.Now - _start).TotalSeconds}");
        }
    }
}