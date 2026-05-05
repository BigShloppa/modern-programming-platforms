using TestFramework;
using TestSubject;

namespace Tests
{
    [TestClass]
    public class FilterDemoTests
    {
        private Calculator _calc;

        [SetUp]
        public void SetUp() => _calc = new Calculator();

        [TestMethod]
        [TestCategory("Math")]
        [Priority(1)]
        [Author("Alice")]
        public void Math_Add_Valid()
        {
            Assert.AreEqual(5, _calc.Add(2, 3));
        }

        [TestMethod]
        [TestCategory("Math")]
        [Priority(2)]
        [Author("Bob")]
        public void Math_Multiply_Valid()
        {
            Assert.AreEqual(6, _calc.Multiply(2, 3));
        }

        [TestMethod]
        [TestCategory("String")]
        [Priority(1)]
        [Author("Alice")]
        public void String_Contains_Valid()
        {
            Assert.Contains("world", "hello world");
        }

        [TestMethod]
        [TestCategory("String")]
        [Priority(3)]
        [Author("Charlie")]
        public void String_DoesNotContain_Valid()
        {
            Assert.DoesNotContain("foo", "hello world");
        }

        [TestMethod]
        [TestCategory("Math")]
        [Priority(1)]
        [Author("Alice")]
        public void Demo_ExpressionAssert()
        {
            int a = 5;
            int b = 3;
            Assert.That(() => a + b == 9);
        }
    }
}