using System;

namespace TestFramework
{
    public class AssertException : Exception
    {
        public AssertException(string message) : base(message) { }
    }

    public class TestException : Exception
    {
        public TestException(string message) : base(message) { }
    }
}