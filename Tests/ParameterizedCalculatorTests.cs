using System.Collections.Generic;
using TestFramework;
using TestSubject;

namespace Tests
{
    [TestClass]
    public class ParameterizedCalculatorTests
    {
        private Calculator _calc;

        [SetUp]
        public void SetUp() => _calc = new Calculator();

        [ParameterizedTest]
        public IEnumerable<object[]> AddTestCases()
        {
            yield return new object[] { 1, 2, 3 };
            yield return new object[] { -1, 1, 0 };
            yield return new object[] { 0, 0, 0 };
            yield return new object[] { 100, 200, 300 };
        }

        [ParameterizedTest]
        public IEnumerable<object[]> MultiplyTestCases()
        {
            yield return new object[] { 2, 3, 6 };
            yield return new object[] { -2, 3, -6 };
            yield return new object[] { 0, 5, 0 };
        }
    }
}