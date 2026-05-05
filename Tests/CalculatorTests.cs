using System;
using TestFramework;
using TestSubject;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class CalculatorTests
    {
        private Calculator _calc;

        [SetUp]
        public void SetUp()
        {
            _calc = new Calculator();
        }

        [TearDown]
        public void TearDown()
        {
            _calc = null;
        }

        [TestMethod]
        public void Add_ShouldReturnSum()
        {
            var result = _calc.Add(2, 3);
            Assert.AreEqual(5, result);
        }

        [TestMethod]
        public void Subtract_ShouldReturnDifference()
        {
            var result = _calc.Subtract(5, 3);
            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void Multiply_ShouldReturnProduct()
        {
            var result = _calc.Multiply(4, 5);
            Assert.AreEqual(20, result);
        }

        [TestMethod]
        public void Divide_ShouldReturnQuotient()
        {
            var result = _calc.Divide(10, 2);
            Assert.AreEqual(5.0, result);
        }

        [TestMethod]
        public void Divide_ByZeroThrowsException()
        {
            Assert.Throws<DivideByZeroException>(() => _calc.Divide(10, 0));
        }

        [TestMethod]
        public async Task SlowAddAsync_ShouldReturnSum()
        {
            var result = await _calc.SlowAddAsync(3, 7);
            Assert.AreEqual(10, result);
        }

        [TestMethod]
        public void IsTrueCondition()
        {
            bool flag = true;
            Assert.IsTrue(flag);
        }

        [TestMethod]
        public void IsFalseCondition()
        {
            bool flag = false;
            Assert.IsFalse(flag);
        }

        [TestMethod]
        public void IsNullCheck()
        {
            object obj = null;
            Assert.IsNull(obj);
        }

        [TestMethod]
        public void IsNotNullCheck()
        {
            object obj = new object();
            Assert.IsNotNull(obj);
        }

        [TestMethod]
        public void GreaterThan()
        {
            Assert.Greater(10, 5);
        }

        [TestMethod]
        public void LessThan()
        {
            Assert.Less(3, 8);
        }

        [TestMethod]
        public void StringContains()
        {
            Assert.Contains("world", "hello world");
        }

        [TestMethod]
        public void StringDoesNotContain()
        {
            Assert.DoesNotContain("foo", "hello world");
        }
    }
}