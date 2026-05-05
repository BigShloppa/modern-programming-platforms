using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestFramework
{
    public class TestResult
    {
        public string TestClassName { get; set; }
        public string TestMethodName { get; set; }
        public bool Passed { get; set; }
        public string Message { get; set; }
        public double DurationMs { get; set; }
    }
}
